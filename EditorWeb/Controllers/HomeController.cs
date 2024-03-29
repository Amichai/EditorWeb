﻿using DataLibrary;
using DataWorkbench.IInputLoader;
using EquationEditor;
using EquationEditor.InputModules;
using Newtonsoft.Json.Linq;
using Roslyn.Compilers;
using Roslyn.Scripting;
using Roslyn.Scripting.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace EditorWeb.Controllers {
    public class HomeController : Controller {
        public ActionResult Index() {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";
            if (Session["python"] == null) {
                IInputModule python = new IronPython();
                Session.Add("python", python);
            }
            loadScriptEngine();
            return View();
        }

        public ActionResult About() {
            ViewBag.Message = "Your app description page.";
            return View();
        }

        public ActionResult Contact() {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        static IInputModule latex = new LatexParser();

        private string wrapInDiv(string html) {
            string toReturn = "<div style='padding:10px;word-wrap:break-word;white-space:pre;'>" + html + "</div>";
            return toReturn;
        }

        [HttpPost]
        public ActionResult AppendPython(string inputText, string lineNumber) {
            if (string.IsNullOrWhiteSpace(inputText)) {
                return Json("");
            }
            try {
                var result = (Session["python"] as IronPython).ForHtml(inputText);
                if (string.IsNullOrWhiteSpace(result)) {
                    CSharpAssign(result, lineNumber);                    
                    return Json(inputText);
                } else {
                    CSharpAssign(result, lineNumber);
                    return Json(result);
                }
            } catch (Exception ex){
                Debug.Print("Error: " + ex.Message);
                return Json(ex.Message);
            }
        }

        private void CSharpAssign(string inputText, string result, string lineNumber) {
            string lastValName = "_" + lineNumber;
            try {
                session.Execute(@"var " + lastValName + " = " + inputText + ";");
            } catch {
                var escapedString = result.Replace("\"", "\"\"");
                var assign = "var " + lastValName + " = @\"" + escapedString + "\";";
                session.Execute(assign);
            }
        }

        private void CSharpAssign(string result, string lineNumber) {
            if (result == null) {
                return;
            }
            string lastValName = "_" + lineNumber;
            var escapedString = result.Replace("\"", "\"\"");
            var assign = "var " + lastValName + " = @\"" + escapedString + "\";";
            session.Execute(assign);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult AppendCSharp(string lineNumber) {
            string inputText = Request.Form[0];
            if (string.IsNullOrWhiteSpace(inputText)) {
                return Json("");
            }
            try {   
                var result = session.Execute(inputText);
                if (result == null) {
                    return Json(inputText);
                }
                CSharpAssign(inputText, result.ToString(), lineNumber);
                if (inputText.Last() == ';') {
                    return Json(inputText);
                }
                return Json(result.ToString());
            } catch (Exception ex) {
                return Json(ex.Message);
            }
        }

        private void loadScriptEngine() {
            engine = new ScriptEngine();
            engine.AddReference(typeof(System.Linq.Enumerable).Assembly.Location);
            engine.AddReference(typeof(JObject).Assembly.Location);
            engine.AddReference(typeof(XElement).Assembly.Location);
            engine.AddReference(typeof(FunctionLibrary).Assembly.Location);
            
            ///Untested
            //engine.AddReference(typeof(Uri).Assembly.Location);
            //engine.AddReference(typeof(XmlAttribute).Assembly.Location);

            engine.AddReference(new MetadataFileReference(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll"));
            engine.AddReference(new MetadataFileReference(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xml.dll"));


            engine.ImportNamespace("System");
            engine.ImportNamespace("System.Collections.Generic");
            engine.ImportNamespace("System.Linq");
            engine.ImportNamespace("System.Text");
            engine.ImportNamespace("System.Diagnostics");
            engine.ImportNamespace("Newtonsoft.Json.Linq");
            engine.ImportNamespace("System.Xml.Linq");

            session = engine.CreateSession(this);

            session.Execute("using DataLibrary;");
        }

        private static Session session;
        private static ScriptEngine engine;

        static List<IInputLoader> loaders = new List<IInputLoader>() {
            new XMLLoader(),
            new JSONLoader(),
            new URLLoader(),
        };

        [HttpPost]
        public ActionResult XMLJSON(string inputText, string lineNumber) {
            if (string.IsNullOrWhiteSpace(inputText)) {
                return Json("");
            }

            Type dataType;

            foreach (var loader in loaders) {
                string content = null;
                try {
                    content = inputText.GetContent();
                } catch {

                }

                try {
                    var result = loader.Load(inputText, session, content, out dataType);
                    if (inputText.Last() != ';') {
                        return Json(result.ToString());
                    } else {
                        return Json("Success.");
                    }
                } catch (Exception ex) {
                    Debug.Print("Error: " + ex.Message);
                }
            }

            return Json("Failed to load.");
        }

        [HttpPost]
        public ActionResult AppendLatex(string inputText) {
            if (string.IsNullOrWhiteSpace(inputText)) {
                return Json("");
            }

            try {
                var result = latex.ForHtml(inputText);
                if (string.IsNullOrWhiteSpace(result)) {
                    return Json(inputText);
                } else {
                    //string toRender = wrapInDiv(result);
                    return Json(result);
                }
            } catch (Exception ex) {
                Debug.Print("Error: " + ex.Message);
                return Json(ex.Message);
            }
        }
    }
}
