using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace book.Tools
{
    public interface ITool
    {
        void OnCompletion(Run run);
        string GetPrompt(RunInfo info);
    }
}
