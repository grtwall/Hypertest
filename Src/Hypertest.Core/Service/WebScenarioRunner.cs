﻿#region License

// Copyright (c) 2014 Chandramouleswaran Ravichandran
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Hypertest.Core.Attributes;
using Hypertest.Core.Interfaces;
using Hypertest.Core.Results;
using Hypertest.Core.Tests;
using Hypertest.Core.Utils;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using Wide.Interfaces;
using Wide.Interfaces.Services;
using Hypertest.Core.Events;

namespace Hypertest.Core.Service
{
    public enum BrowserType
    {
        InternetExplorer,
        Chrome,
        Firefox
    }

    [ScenarioTypes(typeof(WebTestScenario))]
    public class WebScenarioRunner : IRunner
    {
        #region Members
        private readonly Dictionary<String, Variable> _globals;
        private TestResultModel _result;
        private WebTestScenario _scenario;
        private IWorkspace _workspace;
        private IUnityContainer _container;
        private IEventAggregator _aggregator;
        #endregion

        #region CTOR

        public WebScenarioRunner(AbstractWorkspace workspace, IUnityContainer container, IEventAggregator aggregator)
        {
            _globals = new Dictionary<string, Variable>();
            _workspace = workspace;
            _container = container;
            _aggregator = aggregator;
        }

        #endregion

        #region IRunner
        public TestResultModel Result
        {
            get { return _result; }
        }

        public void Initialize(TestScenario scenario)
        {
            lock (this)
            {
                if (this.IsRunning == false)
                {
                    this.IsRunning = true;
                    _result = new TestResultModel();
                    _result.Scenario = scenario;
                    _scenario = scenario as WebTestScenario;
                    scenario.PauseStateManager();
                    scenario.SetDirty(false);

                    var wtrvm = _container.Resolve<WebTestCurrentResultViewModel>();
                    wtrvm.SetModel(_result);
                    var view = _container.Resolve<WebTestResultView>();
                    view.DataContext = _result;
                    wtrvm.SetView(view);
                    _workspace.Documents.Add(wtrvm);
                    _workspace.ActiveDocument = wtrvm;


                    Task.Factory.StartNew(() =>
                    {
                        this.BackRun();
                        this.WorkComplete();
                    });
                }
            }
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Wait(int milliseconds)
        {
            throw new NotImplementedException();
        }

        public bool AddVariable(Variable variable)
        {
            return InternalAddVariable(variable);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void CleanUp()
        {
            if (this.Driver != null)
            {
                this.Driver.Quit();
                this.Driver = null;
            }

            //Set the variables back to null
            this._result = null;
            this._scenario = null;
        }

        public Variable GetVariable(string name)
        {
            if (_globals.ContainsKey(name))
            {
                return _globals[name];
            }
            return null;
        }

        public string PrintDebug()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Dumping variables from the current run...");
            foreach (var kvp in _globals)
            {
                Variable v = kvp.Value;
                sb.AppendLine(kvp.Value.ToString());
            }
            sb.Append("Dump complete...");
            return sb.ToString();
        }

        public string UniqueID { get; private set; }
        public string RunFolder { get; private set; }
        public bool IsRunning { get; private set; }
        public IWebDriver Driver { get; private set; }
        #endregion

        #region Methods
        private bool InternalAddVariable(Variable variable, bool force = true)
        {
            if (_globals.ContainsKey(variable.Name))
            {
                if (force)
                {
                    _globals.Remove(variable.Name);
                }
                else
                {
                    _result.Scenario.Log("Variable " + variable.Name + "already exists. Cannot add new variable.", LogCategory.Warn, LogPriority.Low);
                }
            }
            _globals.Add(variable.Name, variable);
            return true;
        }

        private void Create(BrowserType type)
        {
            if (this.Driver != null)
            {
                throw new Exception("Browser already created for this run");
            }

            switch (type)
            {
                case BrowserType.InternetExplorer:
                    this.Driver = new InternetExplorerDriver(FileUtils.DriverPath);
                    break;
                case BrowserType.Chrome:
                    this.Driver = new ChromeDriver(FileUtils.DriverPath);
                    break;
                case BrowserType.Firefox:
                    this.Driver = new FirefoxDriver();
                    break;
            }
        }

        private void WorkComplete()
        {
            _aggregator.GetEvent<RunEndedEvent>().Publish(this);
            this.CleanUp();
            this.IsRunning = false;
        }

        private void BackRun()
        {
            _aggregator.GetEvent<RunStartedEvent>().Publish(this);
            do
            {
                this.UniqueID = DateTime.Now.Ticks.ToString();
            } while (Directory.Exists(FileUtils.AppPath + Path.DirectorySeparatorChar + this.UniqueID));

            this.RunFolder = FileUtils.ResultPath + Path.DirectorySeparatorChar + this.UniqueID;
            Directory.CreateDirectory(this.RunFolder);

            _globals.Clear();
            try
            {
                Create(_scenario.BrowserType);
            }
            catch (Exception ex)
            {
				_scenario.Log(ex.Message, LogCategory.Exception, LogPriority.High);
				_scenario.Log(ex.StackTrace, LogCategory.Exception, LogPriority.High);
				_scenario.ActualResult = TestCaseResult.Failed;
				Dispatcher.CurrentDispatcher.Invoke(() => _scenario.RunState = TestRunState.Done);
				CleanUp();
				return;
            }
            if (_scenario.URL != null)
            {
                this.Driver.Navigate().GoToUrl(_scenario.URL);
            }
            _scenario.Run();
        }
        #endregion
    }
}