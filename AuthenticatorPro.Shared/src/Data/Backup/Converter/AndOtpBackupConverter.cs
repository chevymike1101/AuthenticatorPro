// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data.Generator;
using AuthenticatorPro.Shared.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Data.Backup.Converter
{
    public class AndOtpBackupConverter : BackupConverter
    {
        // Encrypted backups aren't possible just yet. Wait for AesGcm support in Mono
        // https://github.com/mono/mono/issues/19285
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public AndOtpBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }

        public override Task<Backup> ConvertAsync(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var sourceAccounts = JsonConvert.DeserializeObject<List<Account>>(json);

            var authenticators = sourceAccounts.Select(account => account.Convert(IconResolver)).ToList();
            var categories = new List<Category>();
            var bindings = new List<AuthenticatorCategory>();

            // Convoluted loop because Authenticator.CleanSecret might have changed the secret somehow
            for (var i = 0; i < sourceAccounts.Count; i++)
            {
                var sourceAccount = sourceAccounts[i];
                var auth = authenticators[i];

                foreach (var tag in sourceAccount.Tags)
                {
                    var category = categories.FirstOrDefault(c => c.Name == tag);

                    if (category == null)
                    {
                        category = new Category(tag);
                        categories.Add(category);
                    }

                    var binding = new AuthenticatorCategory(auth.Secret, category.Id);
                    bindings.Add(binding);
                }
            }

            return Task.FromResult(new Backup(authenticators, categories, bindings));
        }

        private class Account
        {
            [JsonProperty(PropertyName = "secret")]
            public string Secret { get; set; }

            [JsonProperty(PropertyName = "issuer")]
            public string Issuer { get; set; }

            [JsonProperty(PropertyName = "label")] public string Label { get; set; }

            [JsonProperty(PropertyName = "digits")]
            public int Digits { get; set; }

            [JsonProperty(PropertyName = "type")] public string Type { get; set; }

            [JsonProperty(PropertyName = "algorithm")]
            public string Algorithm { get; set; }

            [JsonProperty(PropertyName = "thumbnail")]
            public string Thumbnail { get; set; }

            [JsonProperty(PropertyName = "period")]
            public int? Period { get; set; }

            [JsonProperty(PropertyName = "counter")]
            public int Counter { get; set; }

            [JsonProperty(PropertyName = "tags")] public List<string> Tags { get; set; }


            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    "TOTP" => AuthenticatorType.Totp,
                    "HOTP" => AuthenticatorType.Hotp,
                    "STEAM" => AuthenticatorType.SteamOtp,
                    _ => throw new ArgumentOutOfRangeException(nameof(Type))
                };

                var algorithm = Algorithm switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentOutOfRangeException(nameof(Algorithm))
                };

                string issuer;
                string username;

                if (String.IsNullOrEmpty(Issuer))
                {
                    issuer = Label;
                    username = null;
                }
                else
                {
                    issuer = Issuer;
                    username = Label;
                }

                return new Authenticator
                {
                    Secret = Authenticator.CleanSecret(Secret, type),
                    Issuer = issuer,
                    Username = username,
                    Digits = Digits,
                    Period = Period ?? type.GetDefaultPeriod(),
                    Counter = Counter,
                    Type = type,
                    Algorithm = algorithm,
                    Icon = iconResolver.FindServiceKeyByName(Thumbnail)
                };
            }
        }
    }
}