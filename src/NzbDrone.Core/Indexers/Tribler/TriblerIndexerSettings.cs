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
        }
    }

    public class TriblerIndexerSettings : IIndexerSettings
    {
        private static readonly TriblerSettingsValidator Validator = new TriblerSettingsValidator();

        public TriblerIndexerSettings()
        {
            BaseUrl = "http://localhost:52194";
        }

        [FieldDefinition(1, Label = "BaseUrl", Type = FieldType.Textbox, HelpText = "The url for the tribler rest interface, eg http://[host]:[port]/[urlBase], defaults to 'http://localhost:52194'")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "ApiKey", Type = FieldType.Textbox, Privacy = PrivacyLevel.Password, HelpText = "Api key, found in %APPDATA%\\Roaming\\.Tribler\\7.10\\triblerd.conf, the api key is [api].key, NOT [http_api].key")]
        public string ApiKey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
