# OpenANPR to Teams Webhook Alert Proxy (ANPR2Teams)

## What does it do?
 1.  Accepts OpenALPR alert payloads and 
 2.  Sends a MessageCard to a Teams Webhook
   and Power Automate HTTP trigger for further user processing.

## How to make it work?
You will need to update 2 things (and a 3rd, optional thing):

 * Teams Webhook URL - webhookUrl - Line 58

* Address for your OpenALPR installation (if not using Rekor's cloud service) - a2tConstructedViewUrl - Line 101

* (Optional) Culture listed for locale-specific datetime format (ie, change en-AU to your locale) - a2tDateTime - Line 108
