// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityModel.OidcClient.DPoP;

/// <summary>
/// Message handler to create and send DPoP proof tokens.
/// </summary>
public class ProofTokenMessageHandler : DelegatingHandler
{
    private readonly DPoPProofTokenFactory _proofTokenFactory;
    private string? _nonce;

    /// <summary>
    /// Constructor
    /// </summary>
    public ProofTokenMessageHandler(string proofKey, HttpMessageHandler innerHandler)
    {
        _proofTokenFactory = new DPoPProofTokenFactory(proofKey);
        InnerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CreateProofToken(request);

        var response = base.SendAsync(request, cancellationToken).GetAwaiter().GetResult();

        var dPoPNonce = response.GetDPoPNonce();

        if (dPoPNonce != _nonce)
        {
            // nonce is different, so old onto it
            _nonce = dPoPNonce;

            // failure and nonce was differnet so we retry
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();

                CreateProofToken(request);

                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        return response;
    }

    private void CreateProofToken(HttpRequestMessage request)
    {
        var proofRequest = new DPoPProofRequest
        {
            Method = request.Method.ToString(),
            Url = request.GetDPoPUrl(),
            DPoPNonce = _nonce
        };

        if (request.Headers.Authorization != null &&
            OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP.Equals(request.Headers.Authorization.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            proofRequest.AccessToken = request.Headers.Authorization.Parameter;
        }

        var proof = _proofTokenFactory.CreateProofToken(proofRequest);

        request.SetDPoPProofToken(proof.ProofToken);
    }
}