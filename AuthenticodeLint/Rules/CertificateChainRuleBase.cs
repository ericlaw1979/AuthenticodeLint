﻿using System.Security.Cryptography.X509Certificates;

namespace AuthenticodeLint.Rules
{
    public abstract class CertificateChainRuleBase : IAuthenticodeSignatureRule
    {
        public abstract int RuleId { get; }
        public abstract string RuleName { get; }
        public abstract string ShortDescription { get; }

        protected abstract bool ValidateChain(Signature signer, X509Chain chain, SignatureLogger verboseWriter);

        public RuleResult Validate(Graph<Signature> graph, SignatureLogger verboseWriter, CheckConfiguration configuration)
        {
            var signatures = graph.VisitAll();
            var result = RuleResult.Pass;
            foreach (var signature in signatures)
            {
                var certificates = signature.AdditionalCertificates;
                using (var chain = new X509Chain())
                {
                    chain.ChainPolicy.ExtraStore.AddRange(certificates);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    //The purpose of this check is not to validate the chain, completely.
                    //The chain is needed so we know which certificate is the root and intermediates so we know which to validate and which not to validate.
                    //It is possible to have a valid Authenticode signature if the certificate is expired but was
                    //timestamped while it was valid. In this case we still want to successfully build a chain to perform validation.
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;
                    bool success = chain.Build(signature.SignerInfo.Certificate);
                    if (!success)
                    {
                        verboseWriter.LogSignatureMessage(signature.SignerInfo, $"Cannot build a chain successfully with signing certificate {signature.SignerInfo.Certificate.SerialNumber}.");
                        result = RuleResult.Fail;
                        continue;
                    }
                    if (!ValidateChain(signature, chain, verboseWriter))
                    {
                        result = RuleResult.Fail;
                    }
                }
            }
            return result;
        }
    }
}
