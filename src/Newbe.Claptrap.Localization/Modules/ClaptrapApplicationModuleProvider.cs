using System.Collections.Generic;

namespace Newbe.Claptrap.Localization.Modules
{
    public class ClaptrapApplicationModuleProvider : IClaptrapApplicationModuleProvider
    {
        public IEnumerable<IClaptrapApplicationModule> GetClaptrapApplicationModules()
        {
            yield return new LocalizationModule();
        }
    }
}