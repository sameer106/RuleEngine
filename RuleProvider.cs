using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using Newtonsoft.Json.Bson;
using RuleEngine.Core.Domain;
using RuleEngine.Core.Helper;
using RuleEngine.DataObject;
using ServiceStack.Common;
using ServiceStack.Common.Extensions;
using ServiceStack.Logging;
using ServiceStack.Logging.Log4Net;
using ServiceStack.Text;

namespace RuleEngine.Core
{
    public class RuleProvider
    {
        #region "Private Variables"
        private static List<CommunicationRules> communicationRules;
        private static List<CorporateRules> corporateRules;
        private static RuleProvider _instance = null;
        private static object _locker = new object();
        private static long ThreadSleepTimeInMS = 5 * 60 * 1000;
        private static ILogFactory _LogFactory = null;
        private static DateTime cutOffDateTime;

        #endregion "Private Variables"

        private RuleProvider()
        {
            communicationRules = new List<CommunicationRules>();
            corporateRules = new List<CorporateRules>();
        }

        public static RuleProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_locker)
                    {
                        if (_instance == null)
                        {
                            _instance = new RuleProvider();
                            _LogFactory = new Log4NetFactory(true);
                            _instance.FetchRuleFromDB();
                            
                            corporateRules.AddRange(_instance.GetRuleObjectfromJson(communicationRules));
                            Task.Factory.StartNew(() => _instance.GetUpdatedCorporateRule());
                        }
                        
                    }
                }

                return _instance;
            }
        }

        public void GetUpdatedCorporateRule()
        {
            while (true)
            {
                 try
                {
                    FetchRuleFromDB();
                    cutOffDateTime = System.DateTime.Now;
                    corporateRules = GetRuleObjectfromJson(communicationRules);
                     if (_LogFactory != null)
                         _LogFactory.GetLogger("Global").Info("RULE_ENGINE: Get the updated rules and assign to Rule Engine");
                }
                 catch (Exception ex)
                 {
                     if (_LogFactory != null)
                         _LogFactory.GetLogger("Global").Error("RULE_ENGINE: Failed to get updated Rules due to exception", ex);
                     
                 }
                 finally
                 {
                     try
                     {
                         Thread.Sleep(TimeSpan.FromMilliseconds(ThreadSleepTimeInMS));
                     }
                     catch { }
                 }
            }
        }

        //fetch the rules form db
        //get the ruleSet of CorpId or SchemeID
        //Run the filter and return bool
        //if filter return true get the action and update the final actionObject value if action value !=null

        public RuleAction Execute(Request request)
        {
            RuleAction resultRuleAction = new RuleAction();
            
            try
            {
                var corporateStateRules = GetRuleSetsByCorpSchemaIdAndToState(request);
            
                //if no rule than get default rule Set for corporate
                //Default CorporateID = 0
                if (corporateStateRules == null)
                {
                    request.CorpId = 0;
                    corporateStateRules = GetRuleSetsByCorpSchemaIdAndToState(request);
                }

               
                if (corporateStateRules != null)
                foreach (var rules in corporateStateRules)
                {
                    if (rules.filter != null && rules.filter.ruleExpression != null && rules.filter.ruleExpression.Count > 0)
                    {

                        var CompiledRule = PrecompiledRules.CompileRule(request, rules.filter.ruleExpression);

                        //it will evaluate the list of RuleExpression as And condition
                        if (!CompiledRule.Any(x=>!x.Invoke(request)))
                        {
                            if (_LogFactory != null)
                                _LogFactory.GetLogger("Global")
                                    .Info("RULE_ENGINE: Validate the Rule as True for " + rules.SerializeToString());
                            ComposeFinalAction(rules.action, resultRuleAction);
                        }

                    }

                }

                ReplaceCustomerHRTagsByValue(request,resultRuleAction);
                
            }
            catch (Exception ex)
            {
                if (_LogFactory != null)
                    _LogFactory.GetLogger("Global").Error("RULE_ENGINE: Failed to Execute Rule Engine due to exception", ex);
            }
            return resultRuleAction;

        }

        

        public void ReplaceCustomerHRTagsByValue(Request request,RuleAction ruleAction)
        {

            //Replace customer tag by value
            if (ruleAction.toEmailHashSet != null) ruleAction.toEmailHashSet.ReplaceValue(request.CustomerEmail, "customertag");
            if (ruleAction.ccEmailHashSet != null) ruleAction.ccEmailHashSet.ReplaceValue(request.CustomerEmail, "customertag");
            if (ruleAction.bccEmailHashSet != null) ruleAction.bccEmailHashSet.ReplaceValue(request.CustomerEmail, "customertag");
            if (ruleAction.contactNoHashSet != null) ruleAction.contactNoHashSet.ReplaceValue(request.CustomerContact, "customertag");

            //replace HR tag by value
            if (ruleAction.toEmailHashSet != null) ruleAction.toEmailHashSet.ReplaceValue(request.HREmail, "hrtag");
            if (ruleAction.ccEmailHashSet != null) ruleAction.ccEmailHashSet.ReplaceValue(request.HREmail, "hrtag");
            if (ruleAction.bccEmailHashSet != null) ruleAction.bccEmailHashSet.ReplaceValue(request.HREmail, "hrtag");
            if (ruleAction.contactNoHashSet != null) ruleAction.contactNoHashSet.ReplaceValue(request.HRContact, "hrtag");


            //replace Hospital tag by value
            if (ruleAction.toEmailHashSet != null) ruleAction.toEmailHashSet.ReplaceValue(request.HospitalEmail, "hospitaltag");
            if (ruleAction.ccEmailHashSet != null) ruleAction.ccEmailHashSet.ReplaceValue(request.HospitalEmail, "hospitaltag");
            if (ruleAction.bccEmailHashSet != null) ruleAction.bccEmailHashSet.ReplaceValue(request.HospitalEmail, "hospitaltag");
            if (ruleAction.contactNoHashSet != null) ruleAction.contactNoHashSet.ReplaceValue(request.HRContact, "hospitaltag");


            //replace PolicyHolder tag by value
            if (ruleAction.toEmailHashSet != null) ruleAction.toEmailHashSet.ReplaceValue(request.PolicyHolderEmail, "policyholdertag");
            if (ruleAction.ccEmailHashSet != null) ruleAction.ccEmailHashSet.ReplaceValue(request.PolicyHolderEmail, "policyholdertag");
            if (ruleAction.bccEmailHashSet != null) ruleAction.bccEmailHashSet.ReplaceValue(request.PolicyHolderEmail, "policyholdertag");
            if (ruleAction.contactNoHashSet != null) ruleAction.contactNoHashSet.ReplaceValue(request.PolicyHolderContact, "policyholdertag");

          }
        
        public void ComposeFinalAction(RuleAction ruleAction, RuleAction resultRuleAction)
        {
            if (ruleAction.sendEmail)
            {
                resultRuleAction.sendEmail = true;

                resultRuleAction.toEmailHashSet = resultRuleAction.toEmailHashSet.AddHashSet(ruleAction.toEmailHashSet);
                resultRuleAction.ccEmailHashSet = resultRuleAction.ccEmailHashSet.AddHashSet(ruleAction.ccEmailHashSet);
                resultRuleAction.bccEmailHashSet = resultRuleAction.bccEmailHashSet.AddHashSet(ruleAction.bccEmailHashSet);
                resultRuleAction.attachmentHashSet = resultRuleAction.attachmentHashSet.AddHashSet(ruleAction.attachmentHashSet);

                resultRuleAction.emailTemplateId = ruleAction.emailTemplateId;
                resultRuleAction.letterId = ruleAction.letterId;
            }
            else 
            {
                //except email mention in sendEmail = false

                resultRuleAction.toEmailHashSet.ExceptWithNullCheck(ruleAction.toEmailHashSet);
                resultRuleAction.ccEmailHashSet.ExceptWithNullCheck(ruleAction.ccEmailHashSet);
                resultRuleAction.bccEmailHashSet.ExceptWithNullCheck(ruleAction.bccEmailHashSet);
                resultRuleAction.attachmentHashSet.ExceptWithNullCheck(ruleAction.attachmentHashSet);
            }

            if (ruleAction.sendSMS)
            {
                resultRuleAction.sendSMS = true;
                resultRuleAction.contactNoHashSet = resultRuleAction.contactNoHashSet.AddHashSet(ruleAction.contactNoHashSet);
                resultRuleAction.smsTemplateId = ruleAction.smsTemplateId;

            }
            else
            {
                if (resultRuleAction.contactNoHashSet != null && ruleAction.contactNoHashSet != null)
                    resultRuleAction.contactNoHashSet.ExceptWith(ruleAction.contactNoHashSet);
            }

        }

        public List<RuleSet> GetRuleSetsByCorpSchemaIdAndToState(Request request)
        {
            List<RuleSet> corporatStateRuleSet = null;
            CorporateRules corporateRule = null;
            //getting the rule for Corporate or Scheme
            if (corporateRules != null && corporateRules.Count>0)
            {
                corporateRule = new CorporateRules();
                corporateRule = corporateRules.Where(x => x.corpId == request.CorpId && x.schemeId == request.SchemeId).FirstOrDefault();
                
            }

            //get the rules of corporate for the specific ToState of Claim
            if (corporateRule != null && corporateRule.ruleSets != null && corporateRule.ruleSets.Count > 0)
            {
                corporatStateRuleSet = new List<RuleSet>();
                corporatStateRuleSet = corporateRule.ruleSets.Where(corpRule => corpRule.filter.toState == request.ToState).ToList();
                return corporatStateRuleSet;
            }

            return corporatStateRuleSet;
        }

        public void FetchRuleFromDB()
        {
            if (_LogFactory != null)
                _LogFactory.GetLogger("Global").Info("RULE_ENGINE: Call DB to get the Rules ");

            cutOffDateTime = System.DateTime.Now; //set cutOff date time after get the latest rules from DB

            //if CommunicationRules list is null than fetch all else fetch only those which are updated recently
            if (communicationRules.Count == 0)
                communicationRules = DBUtility.GetRules();
            else
            {
                var ListOfRuleModifiedAfterCutOff = DBUtility.GetRules(cutOffDateTime);
                
                //update only those which are modified after Last CutOffTime
                communicationRules.ForEach(
                    r =>
                        r.Rules =
                            ListOfRuleModifiedAfterCutOff.Any(
                                dbRule => dbRule.CommunicationRuleId == r.CommunicationRuleId)?ListOfRuleModifiedAfterCutOff.First(dbRule => dbRule.CommunicationRuleId == r.CommunicationRuleId).Rules : null);
            }
            if (_LogFactory != null)
                _LogFactory.GetLogger("Global").Info("RULE_ENGINE: Successfully Get Rules from DB ");
        }

        #region Utility

        public string RemoveDuplicate(string inputCSV)
        {
            string distinctResponseCSV;
            if (!string.IsNullOrEmpty(inputCSV))
                distinctResponseCSV = string.Join(",",
                    inputCSV.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct());
            else
                distinctResponseCSV = inputCSV;

            return distinctResponseCSV;

        }

        public string GetCSVAppend(string inputCSV, string appendTo)
        {
            string responseCSV = appendTo;

            if (!string.IsNullOrEmpty(inputCSV) && !string.IsNullOrEmpty(responseCSV))
                responseCSV = responseCSV + "," + inputCSV;
            else
                responseCSV = inputCSV;

            return responseCSV;
        }

        public string RemoveFromCSV(string rejectCSV, string removefromCSV)
        {
            string responseCSV = removefromCSV;
            if (!string.IsNullOrEmpty(removefromCSV) && !string.IsNullOrEmpty(rejectCSV))
            {
                var rejectList = rejectCSV.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                responseCSV = string.Join(",", removefromCSV.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Except(rejectList));
            }

            return responseCSV;
        }

        public List<CorporateRules> GetRuleObjectfromJson(List<CommunicationRules> communicationRules)
        {
            List<CorporateRules> objList = new List<CorporateRules>();
            if (communicationRules != null && communicationRules.Count > 0)
            {
                foreach (var commRule in communicationRules)
                {
                    objList.Add(GetRuleObjectfromJson(commRule.Rules));
                }
            }

            return objList;
        }

        public CorporateRules GetRuleObjectfromJson(string jsonRule)
        {
            CorporateRules obj = new CorporateRules();
            try
            {
                obj = (CorporateRules)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonRule, typeof(CorporateRules));
                if (_LogFactory != null)
                    _LogFactory.GetLogger("Global").Info("RULE_ENGINE: Converted Json to Object");

            }
            catch (Exception ex)
            {
                if (_LogFactory != null)
                    _LogFactory.GetLogger("Global").Error("RULE_ENGINE: Failed to convert Json Rule to CorporateRules Object", ex);
            }


            return obj;
        }

        #endregion

        //public string MockCorporateRules()
        //{
        //    return "{ \"rules\": [{ \"corpId\": 1, \"schemeId\": null, \"ruleSets\": [{ \"filter\": { \"fromState\": 0, \"toState\": 1, \"ruleExpression\": { \"propertyName\": \"Amount\", \"operation\": \"GreaterThan\", \"value\": \"1000\" } }, \"action\": { \"sendEmail\": true, \"toEmailcsv\": \"abc@tcs.com\", \"ccEmailcsv\": \"cc@abc.com\", \"bccEmailcsv\": null, \"EmailTemplateId\": 1, \"sendSMS\": false, \"contactNocsv\": null, \"SmsTemplateId\": 0 } }, { \"filter\": { \"fromState\": 0, \"toState\": 1, \"ruleExpression\": { \"propertyName\": \"Amount \", \"operation\": \"GreaterThan\", \"value\": \"800 \" } }, \"action\": { \"sendEmail\": true, \"toEmailcsv\": \"800@abc.com,abc@tcs.com\", \"ccEmailcsv\": null, \"bccEmailHashSet\": null, \"EmailTemplateId\": 1, \"sendSMS\": true, \"contactNoHashSet\": \"9999999999\", \"SmsTemplateId\": 1 } }, { \"filter\": { \"fromState\": 0, \"toState\": 1, \"ruleExpression\": { \"propertyName\": \"Amount \", \"operation\": \"Equal\", \"value\": \"11000 \" } }, \"action\": { \"sendEmail\": false, \"toEmailHashSet\": \"abc@tcs.com\", \"ccEmailHashSet\": null, \"bccEmailHashSet\": null, \"EmailTemplateId\": 1, \"sendSMS\": true, \"contactNoHashSet\": \"9999999999\", \"SmsTemplateId\": 1 } }] }, { \"corpId\": 0, \"schemeId\": null, \"ruleSets\": [{ \"filter\": { \"fromState\": 0, \"toState\": 1, \"ruleExpression\": { \"propertyName\": \"Amount\", \"operation\": \"GreaterThan\", \"value\": \"1000\" } }, \"action\": { \"sendEmail\": true, \"toEmailHashSet\": \"abc@tcs.com\", \"ccEmailcsv\": null, \"bccEmailcsv\": null, \"EmailTemplateId\": 1, \"sendSMS\": false, \"contactNocsv\": null, \"SmsTemplateId\": 0 } }, { \"filter\": { \"fromState\": 0, \"toState\": 0, \"ruleExpression\": { \"propertyName\": \"Amount\", \"operation\": \"GreaterThan\", \"value\": \"1000\" } }, \"action\": { \"sendEmail\": false, \"toEmailcsv\": null, \"ccEmailcsv\": null, \"bccEmailcsv\": null, \"EmailTemplateId\": 0, \"sendSMS\": false, \"contactNocsv\": null, \"SmsTemplateId\": 0 } }] }] }";
        //}

    }

    public static class CustomExtension
    {
        public static HashSet<T> AddHashSet<T>(this HashSet<T> addToSet, HashSet<T> valueset)
        {
            if (addToSet != null)
            {
                if(valueset != null)
                addToSet.UnionWith(valueset);
            }
            else
                addToSet = valueset;

            return addToSet;
        }


        public static HashSet<T> ExceptWithNullCheck<T>(this HashSet<T> sourceHashSet, HashSet<T> excepSet)
        {
            if (sourceHashSet != null && excepSet != null)
                sourceHashSet.ExceptWith(excepSet);   
            return sourceHashSet;
        }

        public static HashSet<T> ReplaceValue<T>(this HashSet<T> sourceHashSet, T newValue, T tag)
        {
            if (sourceHashSet != null)
            {
                if (sourceHashSet.Contains(tag))
                {
                    sourceHashSet.Add(newValue);
                    sourceHashSet.Remove(tag);
                }
            }

            return sourceHashSet;
        }

    }
}
