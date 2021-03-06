﻿using System;
using System.Linq;

namespace AuthenticodeLint.Rules
{
    public class NoUnknownUnsignedAttibuteRule : IAuthenticodeSignatureRule
    {
        public int RuleId { get; } = 10009;

        public string RuleName { get; } = "No Unknown Unsigned Attributes";

        public string ShortDescription { get; } = "Checks for the presence of unsigned attributes with unknown an OID.";

        private static string[] _trustedUnsignedAttributes = new[]
        {
            KnownOids.AuthenticodeCounterSignature,
            KnownOids.Rfc3161CounterSignature,
            KnownOids.NestedSignatureOid
        };

        public RuleResult Validate(Graph<Signature> graph, SignatureLogger verboseWriter, CheckConfiguration configuration)
        {
            var signatures = graph.VisitAll();
            var result = RuleResult.Pass;
            foreach(var signature in signatures)
            {
                var signer = signature.SignerInfo;
                var counterSignatures = GraphBuilder.WalkCounterSignatures(signature);
                foreach(var counterSignature in counterSignatures.VisitAll())
                {
                    foreach (var attribute in counterSignature.UnsignedAttributes)
                    {
                        if (!_trustedUnsignedAttributes.Contains(attribute.Oid.Value))
                        {
                            result = RuleResult.Fail;
                            var displayName = attribute.Oid.FriendlyName ?? "<no friendly name>";
                            verboseWriter.LogSignatureMessage(signer, $"Signature contains counter signer with unknown unsigned attribute {displayName} ({attribute.Oid.Value}).");
                        }
                    }
                }
                foreach(var attribute in signer.UnsignedAttributes)
                {
                    if (!_trustedUnsignedAttributes.Contains(attribute.Oid.Value))
                    {
                        result = RuleResult.Fail;
                        var displayName = attribute.Oid.FriendlyName ?? "<no friendly name>";
                        verboseWriter.LogSignatureMessage(signer, $"Signature contains unknown unsigned attribute {displayName} ({attribute.Oid.Value}).");
                    }
                }
            }
            return result;
        }
    }
}
