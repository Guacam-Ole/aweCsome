using System;
using System.Collections.Generic;
using System.Web.Mvc;
using AweCsome.Buffer;
using AweCsome.Interfaces;
using Microsoft.SharePoint.Client;

namespace BufferTestWeb.Controllers
{
    public class HomeController : Controller
    {
        [SharePointContextFilter]
        public ActionResult Index()
        {
            User spUser = null;

            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    spUser = clientContext.Web.CurrentUser;

                    clientContext.Load(spUser, user => user.Title);

                    clientContext.ExecuteQuery();

                    ViewBag.UserName = spUser.Title;
                }
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult TestBuffer()
        {
            IAweCsomeHelpers helper = new AweCsome.AweCsomeHelpers();
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                AweCsomeTable bufferTable = new AweCsomeTable(new AweCsome.AweCsomeTable(clientContext), helper, "bufferTest");

                bufferTable.Queue.EmptyCommandCollection();

                var myself = clientContext.Web.CurrentUser;
                clientContext.Load(myself);
                clientContext.ExecuteQuery();


                //bufferTable.Queue.AddCommand(new Command
                //{
                //    Action = Command.Actions.CreateTable,
                //    TableName = nameof(LHIND.Mydea.Entities.Modules.Project),
                //    State = Command.States.Pending,
                //    FullyQualifiedName = typeof(LHIND.Mydea.Entities.Modules.Project).FullName
                //});


                //table.Queue.AddCommand(new Command
                //{
                //    Action = Command.Actions.CreateTable,
                //    TableName = nameof(LHIND.Mydea.Entities.Modules.InnovationIdea.InnovationIdea),
                //    State = Command.States.Pending,
                //    FullyQualifiedName = typeof(LHIND.Mydea.Entities.Modules.InnovationIdea.InnovationIdea).FullName
                //});

                bufferTable.Empty<LHIND.Mydea.Entities.Modules.InnovationIdea.InnovationIdea>();
                bufferTable.Empty<LHIND.Mydea.Entities.Modules.Project>();

                var idea = new LHIND.Mydea.Entities.Modules.InnovationIdea.InnovationIdea
                {
                    Id = -1,
                    Title = "Horst",
                    SponsoringDeadline = DateTime.Now.AddDays(22),
                    Owner = new KeyValuePair<int, string>(myself.Id,null), // clientContext.Web.CurrentUser.Id
                    Author=new KeyValuePair<int, string>(3,""),
                    Module = nameof(LHIND.Mydea.Entities.Modules.InnovationIdea)
                };
                bufferTable.InsertItem(idea);

                var project = new LHIND.Mydea.Entities.Modules.Project
                {
                    Id = -1,
                    Module = nameof(LHIND.Mydea.Entities.Modules.InnovationIdea),
                    ParentId = -1,
                    Title="Rüdiger",
                     FundingDeadline=DateTime.Now.AddDays(42),
                    Author = new KeyValuePair<int, string>(3, ""),
                };
                bufferTable.InsertItem(project);

                var stored= bufferTable.SelectItemById<LHIND.Mydea.Entities.Modules.InnovationIdea.InnovationIdea>(-1);
                //table.Queue.AddCommand(new Command
                //{
                //    Action = Command.Actions.Insert,
                //    TableName = nameof(LHIND.Mydea.Entities.Modules.InnovationIdea.InnovationIdea),
                //    State = Command.States.Pending,
                //    FullyQualifiedName = typeof(LHIND.Mydea.Entities.Modules.InnovationIdea.InnovationIdea).FullName,
                //    ItemId = idea.Id
                //});
                bufferTable.Queue.Sync(typeof(LHIND.Mydea.Entities.Image));
            }
            return null;
        }
    }
}
