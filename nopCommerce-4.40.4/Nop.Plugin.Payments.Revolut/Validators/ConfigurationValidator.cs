using FluentValidation;
using Nop.Plugin.Payments.Revolut.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.Revolut.Validators
{
    /// <summary>
    /// Represents configuration model validator
    /// </summary>
    public class ConfigurationValidator : BaseNopValidator<ConfigurationModel>
    {
        #region Ctor

        public ConfigurationValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.ApiKey)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Revolut.Fields.ApiKey.Required"))
                .When(model => model.UseSandbox || !model.UseSandbox);
            //.When(model => !model.UseSandbox);

            RuleFor(model => model.CreditCardTitle)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Revolut.Fields.CreditCardTitle.Required"))
                .When(model => model.UseCreditCard);

            RuleFor(model => model.RevolutPayTitle)
                .NotEmpty()
                .WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Revolut.Fields.RevolutPayTitle.Required"))
                .When(model => model.UseRevolutPay);
                
        }

        #endregion
    }
}