using carestream.core.dtos.vitals;
using System.Threading.Tasks;

namespace carestream.core.interfaces.services
{
    public interface INurseDashboardService
    {
        Task<NurseDashboardViewModel> GetDashboardViewModelAsync();
    }
}