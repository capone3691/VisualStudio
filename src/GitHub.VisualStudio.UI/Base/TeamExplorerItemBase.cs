﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitHub.Api;
using GitHub.Models;
using GitHub.Primitives;
using GitHub.Services;
using GitHub.VisualStudio.Helpers;
using NullGuard;

namespace GitHub.VisualStudio.Base
{
    public class TeamExplorerItemBase : TeamExplorerGitRepoInfo
    {
        readonly ISimpleApiClientFactory apiFactory;
        protected ITeamExplorerServiceHolder holder;

        ISimpleApiClient simpleApiClient;
        [AllowNull]
        public ISimpleApiClient SimpleApiClient
        {
            [return: AllowNull] get { return simpleApiClient; }
            set
            {
                if (simpleApiClient != value && value == null)
                    apiFactory.ClearFromCache(simpleApiClient);
                simpleApiClient = value;
            }
        }

        protected ISimpleApiClientFactory ApiFactory => apiFactory;

        public TeamExplorerItemBase(ITeamExplorerServiceHolder holder)
        {
            this.holder = holder;
        }

        public TeamExplorerItemBase(ISimpleApiClientFactory apiFactory, ITeamExplorerServiceHolder holder)
        {
            this.apiFactory = apiFactory;
            this.holder = holder;
        }

        public virtual void Initialize(IServiceProvider serviceProvider)
        {
#if DEBUG
            //VsOutputLogger.WriteLine("{0:HHmmssff}\t{1} Initialize", DateTime.Now, GetType());
#endif
            ServiceProvider = serviceProvider;
            Debug.Assert(holder != null, "Could not get an instance of TeamExplorerServiceHolder");
            if (holder == null)
                return;
            holder.ServiceProvider = ServiceProvider;
            SubscribeToRepoChanges();
#if DEBUG
            //VsOutputLogger.WriteLine("{0:HHmmssff}\t{1} Initialize DONE", DateTime.Now, GetType());
#endif
        }


        public virtual void Execute()
        {
        }

        public virtual void Invalidate()
        {
        }

        void SubscribeToRepoChanges()
        {
            holder.Subscribe(this, (ISimpleRepositoryModel repo) =>
            {
                ActiveRepo = repo;
                RepoChanged();
            });
        }

        void Unsubscribe()
        {
            holder.Unsubscribe(this);
            if (ServiceProvider != null)
                holder.ClearServiceProvider(ServiceProvider);
        }

        protected virtual void RepoChanged()
        {
            var repo = ActiveRepo;
            if (repo != null)
            {
                var uri = repo.CloneUrl;
                if (uri?.RepositoryName != null)
                {
                    ActiveRepoUri = uri;
                    ActiveRepoName = uri.NameWithOwner;
                }
            }
        }

        protected async Task<bool> IsAGitHubRepo()
        {
            var uri = ActiveRepoUri;
            if (uri == null)
                return false;

            SimpleApiClient = apiFactory.Create(uri);

            var isdotcom = HostAddress.IsGitHubDotComUri(uri.ToRepositoryUrl());
            if (!isdotcom)
            {
                var repo = await SimpleApiClient.GetRepository();
                return (repo.FullName == ActiveRepoName || repo.Id == 0) && SimpleApiClient.IsEnterprise();
            }
            return isdotcom;
        }

        bool disposed;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    Unsubscribe();
                    disposed = true;
                }
            }
            base.Dispose(disposing);
        }

        bool isEnabled;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set { isEnabled = value; this.RaisePropertyChange(); }
        }

        bool isVisible;
        public bool IsVisible
        {
            get { return isVisible; }
            set { isVisible = value; this.RaisePropertyChange(); }
        }

        string text;
        [AllowNull]
        public string Text
        {
            get { return text; }
            set { text = value; this.RaisePropertyChange(); }
        }

    }
}