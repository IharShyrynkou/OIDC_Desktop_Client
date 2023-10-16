﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IdentityModel.OidcClient.DPoP;

/// <summary>
/// Used to create DPoP proof tokens.
/// </summary>
public class DPoPProofTokenFactory
{
    private readonly JsonWebKey _jwk;

    /// <summary>
    /// Constructor
    /// </summary>
    public DPoPProofTokenFactory(string proofKey)
    {
        _jwk = new JsonWebKey(proofKey);
    }

    /// <summary>
    /// Creates a DPoP proof token.
    /// </summary>
    public DPoPProof CreateProofToken(DPoPProofRequest request)
    {
        var jsonWebKey = _jwk;// new JsonWebKey(request.DPoPJsonWebKey);

        // jwk: representing the public key chosen by the client, in JSON Web Key (JWK) [RFC7517] format,
        // as defined in Section 4.1.3 of [RFC7515]. MUST NOT contain a private key.
        object jwk;
        if (string.Equals(jsonWebKey.Kty, JsonWebAlgorithmsKeyTypes.EllipticCurve))
        {
            jwk = new
            {
                kty = jsonWebKey.Kty,
                x = jsonWebKey.X,
                y = jsonWebKey.Y,
                crv = jsonWebKey.Crv
            };
        }
        else if (string.Equals(jsonWebKey.Kty, JsonWebAlgorithmsKeyTypes.RSA))
        {
            jwk = new
            {
                kty = jsonWebKey.Kty,
                e = jsonWebKey.E,
                n = jsonWebKey.N
            };
        }
        else
        {
            throw new InvalidOperationException("invalid key type.");
        }

        var header = new Dictionary<string, object>()
        {
            //{ "alg", "RS265" }, // JsonWebTokenHandler requires adding this itself
            { "typ", JwtClaimTypes.JwtTypes.DPoPProofToken },
            { JwtClaimTypes.JsonWebKey, jwk },
        };

        var payload = new Dictionary<string, object>
        {
            { JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId() },
            { JwtClaimTypes.DPoPHttpMethod, request.Method },
            { JwtClaimTypes.DPoPHttpUrl, request.Url },
            { JwtClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        };

        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            // ath: hash of the access token. The value MUST be the result of a base64url encoding 
            // the SHA-256 hash of the ASCII encoding of the associated access token's value.
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(request.AccessToken));
            var ath = Base64Url.Encode(hash);

            payload.Add(JwtClaimTypes.DPoPAccessTokenHash, ath);
        }

        if (!string.IsNullOrEmpty(request.DPoPNonce))
        {
            payload.Add(JwtClaimTypes.Nonce, request.DPoPNonce!);
        }

        var handler = new JsonWebTokenHandler() { SetDefaultTimesOnTokenCreation = false };
        var key = new SigningCredentials(jsonWebKey, jsonWebKey.Alg);
        var proofToken = handler.CreateToken(JsonSerializer.Serialize(payload), key, header);

        return new DPoPProof { ProofToken = proofToken! };
    }
}
