# RuleEngine

Use this project as library
add this to your project and create rule in json format in database which will be used by RuleEngine.

call Excute method in RuleProvider and pass request object on which you want to apply rule.
in return you will get the action need to perform if the rule get satisfied.

Example Json format :
{
	"corpId": 1020195,
	"schemeId": 0,
	"ruleSets": [{
		"filter": {
			"fromState": 0,
			"toState": 7,
			"ruleExpression": [{
				"propertyName": "Amount",
				"operation": "GreaterThan",
				"value": "1000"
			}]
		},
		"action": {
			"sendEmail": true,
			"toEmailHashSet": ["abc@abc.in"],
			"ccEmailHashSet": null,
			"bccEmailHashSet": null,
			"EmailTemplateId": 1,
			"AttachmentHashSet": null,
			"letterId": 1,
			"sendSMS": false,
			"contactNoHashSet": null,
			"SmsTemplateId": 0
		}
	}]
}


Example request object:
Request request = new Request();
request.Amount = 11000;
request.ToState = 1;
request.CorpId = 1;
request.CustomerContact = "1111111111";
request.CustomerEmail = "customer@xyz.com";


var result = RuleProvider.Instance.Execute(request); // will call the Rule engine and get the action mention in json if filter get satisfied
