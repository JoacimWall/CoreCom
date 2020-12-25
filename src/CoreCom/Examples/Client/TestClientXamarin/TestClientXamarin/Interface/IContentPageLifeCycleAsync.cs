using System.Threading.Tasks;

namespace TestClientXamarin.Interfaces
{
    public interface IContentPageLifeCycleAsync
    {
        Task OnAppearingAsync();
        Task OnDisappearingAsync();
        Task OnBackButtonPressedAsync();
        void OnDisappearing();
        void OnAppearing();
    }
}
