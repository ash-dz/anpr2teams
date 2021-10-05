# anpr2teams

OpenANPR to Teams Webhook Alert Proxy (ANPR2Teams)
   Accepts OpenALPR alert payloads and sends a MessageCard to a Teams Webhook
   and Power Automate HTTP trigger for further user processing.

===============================================================================
** HOW TO MAKE IT WORK **
You will need to update 2 things (and a 3rd, optional thing):

 1  Teams Webhook URL
    (webhookUrl - Line 58)

 2  Address for your OpenALPR installation (if not using Rekor's cloud service)
    (a2tConstructedViewUrl - Line 101)

 3  (Optional) Culture listed for locale-specific datetime format (ie, change en-AU to your locale)
    (a2tDateTime - Line 108)
===============================================================================
