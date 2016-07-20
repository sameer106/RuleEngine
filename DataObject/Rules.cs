using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace RuleEngine.DataObject
{

    public class CorporateRules
    {
        public CorporateRules()
        {
            ruleSets = new List<RuleSet>();
        }

        public int? corpId { get; set; }
        public int? schemeId { get; set; }
        public List<RuleSet> ruleSets { get; set; }
    }

    public class RuleSet
    {
        public RuleSet()
        {
            action = new RuleAction();
            filter = new RuleFilter();
        }

        public RuleFilter filter { get; set; }
        public RuleAction action { get; set; }
    }

    public class RuleFilter
    {
        public int fromState { get; set; }
        public int toState { get; set; }
        public List<RuleExpression> ruleExpression { get; set; }
        
    }

    public class RuleExpression
    {
        public RuleExpression(string _propertyName, ExpressionType _operation ,string _value )
        {
            propertyName = _propertyName;
            operation = _operation;
            value = _value;
        }

        public string propertyName { get; set; }
        public ExpressionType operation { get; set; }
        public string value { get; set; }
    
    }

    public class RuleAction
    {
        //Email
        public bool sendEmail { get; set; }
        public HashSet<string> toEmailHashSet { get; set; }
        public HashSet<string> ccEmailHashSet { get; set; }
        public HashSet<string> bccEmailHashSet { get; set; }
        public int emailTemplateId { get; set; }
        public HashSet<string> attachmentHashSet { get; set; }
        public int letterId { get; set; }

        //SMS
        public bool sendSMS { get; set; }
        public HashSet<string> contactNoHashSet { get; set; }
        public int smsTemplateId { get; set; }
    }


}
