using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bmg.ConsigBoilerplate.Domain
{
    public enum KafkaCluster
    {
        [Description("WEATHER-FORECAST")]
        ConsigBoilerplateProducer = 0
    }
}
