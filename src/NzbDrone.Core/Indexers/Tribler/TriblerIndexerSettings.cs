using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Tribler
{
    public class TriblerSettingsValidator : AbstractValidator<TriblerIndexerSettings>
    {
        public TriblerSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();

            RuleFor(c => c.ApiKey).NotEmpty();

            RuleForEach(c => c.ExtraChannelSubscriptions).NotEmpty();

            RuleForEach(c => c.ExtraChannelSubscriptions).Must(channelSubscription => ValidateChannel(channelSubscription))
                .WithMessage("ChannelSubscription format invalid. A valid channel consists of a publickey in hexidencimal form and a channel id integer in decimal form. Separated by a /");
        }

        public bool ValidateChannel(string channel)
        {
            try
            {
                TriblerChannelSubscription.Parse(channel);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class TriblerIndexerSettings : IIndexerSettings
    {
        private static readonly TriblerSettingsValidator Validator = new TriblerSettingsValidator();

        public TriblerIndexerSettings()
        {
            BaseUrl = "http://localhost:20100";
            FetchSubscribedChannels = false;

            FetchExtraChannels = false;

            ExtraChannelSubscriptions = Array.Empty<string>();
        }

        [FieldDefinition(1, Label = "BaseUrl", Type = FieldType.Textbox, HelpText = "The url for the tribler rest interface, eg http://[host]:[port]/[urlBase], defaults to 'http://localhost:20100'")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "ApiKey", Type = FieldType.Textbox, Privacy = PrivacyLevel.ApiKey, HelpText = "Api key, found in eg %APPDATA%\\Roaming\\.Tribler\\7.10\\triblerd.conf, the api key is [api].key, NOT [http_api].key")]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Advanced = true, Label = "FetchSubscribedChannels", Type = FieldType.Checkbox,  HelpText = "Experimental/Canary: If tribler's subscribed channels should be checked to identify recent torrents.")]
        public bool FetchSubscribedChannels { get; set; }

        [FieldDefinition(4, Advanced = true, Label = "FetchExtraChannels", Type = FieldType.Checkbox, HelpText = "Experimental/Canary: If the extra channels below should be checked to identify recent torrents.")]
        public bool FetchExtraChannels { get; set; }

        [FieldDefinition(10, Advanced = true, Label = "ExtraChannelSubscriptions", HelpText = "Experimental/Canary: Format: 'Channel-Pubkey1>/ChannelID1 ChannelPubkey1/ChannelID2'. List of channels to subscribe to for added torrents (rss emulation). Example: dfgkjhsdfkshfk.../43543905430 fehklsgdghklsgdhklsdhklhklsfd.../3495839405 etc.", Type = FieldType.Tag)]
        public IEnumerable<string> ExtraChannelSubscriptions { get; set; }

        public IEnumerable<TriblerChannelSubscription> GetExtraChannelSubscriptions()
        {
            return ExtraChannelSubscriptions.Select(channel => TriblerChannelSubscription.Parse(channel));
        }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
