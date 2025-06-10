using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hangmantest
{
    public interface IWordGeneratorService
    {
        Task<string> Generate();
    }
}
