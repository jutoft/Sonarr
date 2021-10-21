using NzbDrone.Core.Parser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NzbDrone.Core.Indexers.Tribler
{
    public interface ITriblerSearchRequestGenerator
    {
        IList<ReleaseInfo> Search(string query);
    }
}
