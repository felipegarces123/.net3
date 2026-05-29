using System.ComponentModel;

namespace Bmg.ConsigBoilerplate.Domain
{
    public enum DatabaseConnection
    {
        [Description("Oracle")]
        Oracle = 0,
        [Description("Sybase")]
        Sybase = 1
    }
}
