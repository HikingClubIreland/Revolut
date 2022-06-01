using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Revolut.Services;

namespace Nop.Plugin.Payments.Revolut.Controllers
{
    public class RevolutWebhookController : Controller
    {
        #region Fields

        private readonly RevolutSettings _settings;
        private readonly ServiceManager _serviceManager;

        #endregion

        #region Ctor

        public RevolutWebhookController(RevolutSettings settings,
            ServiceManager serviceManager)
        {
            _settings = settings;
            _serviceManager = serviceManager;
        }

        #endregion

        #region Methods

        [HttpPost]
        public IActionResult WebhookHandler()
        {
            //_serviceManager.HandleWebhook(_settings, Request);
            return Ok();
        }

        #endregion
    }
}