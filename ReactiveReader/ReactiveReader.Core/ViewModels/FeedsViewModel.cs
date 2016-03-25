using System;
using System.Reactive;
using System.Reactive.Linq;
using Akavache;
using Conditions;
using ReactiveUI;
using Splat;

namespace ReactiveReader.Core.ViewModels
{
    using System.Threading.Tasks;

    public interface IFeedsViewModel
    {
        ReactiveCommand<Unit> RemoveBlog { get; }
        ReactiveCommand<Unit> AddBlog { get; }
        ReactiveCommand<Unit> PersistData { get; }
        ReactiveList<IBlogViewModel> Blogs { get; }
        ReactiveCommand<Unit> RefreshAll { get; }
        bool IsLoading { get; }

        IBlogViewModel SelectedBlog { get; }
    }

    public class FeedsViewModel : ReactiveObject, IFeedsViewModel
    {
        IBlobCache Cache { get; }

        public FeedsViewModel(IBlobCache cache = null)
        {
            Cache = cache ?? Locator.Current.GetService<IBlobCache>();

            Cache.GetOrCreateObject(BlobCacheKeys.Blogs, () => new ReactiveList<IBlogViewModel>())
                .Subscribe(blogs => { Blogs = blogs; });

            RefreshAll = ReactiveCommand.CreateAsyncTask(x =>
            {
                foreach (var blog in Blogs)
                {
                    blog.Refresh.InvokeCommand(null);
                }

                return Task.FromResult(Unit.Default);
            });

            RefreshAll.ThrownExceptions.Subscribe(thrownException => { this.Log().Error(thrownException); });

            _isLoading = RefreshAll.IsExecuting.ToProperty(this, x => x.IsLoading);

            PersistData =
                ReactiveCommand.CreateAsyncTask(async x => { await Cache.InsertObject(BlobCacheKeys.Blogs, Blogs); });

            PersistData.ThrownExceptions.Subscribe(thrownException => { this.Log().Error(thrownException); });

            // behaviours

            // when a blog is added or removed, wait for 5 seconds of inactivity before persisting the data as the user may be doing bulk [add|remove] operations.
            this.WhenAnyValue(viewModel => viewModel.Blogs)
                .Throttle(TimeSpan.FromSeconds(5), RxApp.MainThreadScheduler)
                .InvokeCommand(this, viewModel => viewModel.PersistData);

            // When an user adds a new blog to the feed, automatically fetch/cache the contents of the blog.
            // When a blog becomes the selected blog, fetch/cache the contents of the blog.
            this.WhenAnyObservable(viewModel => viewModel.Blogs.ItemsAdded)
                .Merge(this.WhenAnyValue(viewModel => viewModel.SelectedBlog).Where(blogVm => blogVm != null))
                .Subscribe(x => x.Refresh.InvokeCommand(null));

            // post-condition checks
            Condition.Ensures(Cache).IsNotNull();
            Condition.Ensures(RefreshAll).IsNotNull();
            Condition.Ensures(PersistData).IsNotNull();
        }

        public ReactiveCommand<Unit> RemoveBlog { get; }
        public ReactiveCommand<Unit> AddBlog { get; }
        public ReactiveCommand<Unit> PersistData { get; }
        public ReactiveCommand<Unit> RefreshAll { get; }

        ReactiveList<IBlogViewModel> _blogs;
        public ReactiveList<IBlogViewModel> Blogs
        {
            get { return _blogs; }
            private set { this.RaiseAndSetIfChanged(ref _blogs, value); }
        }

        IBlogViewModel _selectedBlog;
        public IBlogViewModel SelectedBlog
        {
            get { return _selectedBlog; }
            set { this.RaiseAndSetIfChanged(ref _selectedBlog, value); }
        }

        readonly ObservableAsPropertyHelper<bool> _isLoading;
        public bool IsLoading
        {
            get { return _isLoading.Value; }
        }
    }
}