using ReactiveUI;
using System.Reactive;

namespace ReactiveReader.Core.ViewModels.DesignTime
{
    public class DesignTimeFeedsViewModel : IFeedsViewModel
    {
        public DesignTimeFeedsViewModel()
        {
            Blogs = new ReactiveList<IBlogViewModel>()
            {
                new DesignTimeBlogViewModel(),
                new DesignTimeBlogViewModel()
            };
        }

        public ReactiveCommand<Unit> RemoveBlog { get; }
        public ReactiveCommand<Unit> AddBlog { get; }
        public ReactiveCommand<Unit> PersistData { get; }
        public ReactiveList<IBlogViewModel> Blogs { get; set; }
        public ReactiveCommand<Unit> RefreshAll { get; }
        public bool IsLoading { get; }
        public IBlogViewModel SelectedBlog { get; }
    }
}