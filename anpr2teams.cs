/*
MIT License

Copyright (c) 2021 Ash de Zylva & Tim Leyden

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

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
*/

#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Globalization;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    // Write line to indicate we started (debug purposes)
    log.LogInformation("C# HTTP trigger function is processing a request.");

    // Update this to the Teams Incoming Webhook URL
    var webhookUrl = "https://empoweryourteam.webhook.office.com/webhookb2/81f4aa91-b557-4f85-b339-ff7a96fbcab3@1c73d81f-3d4c-43aa-931f-c06596737b9e/IncomingWebhook/0603eb14fb9b4ecd933c72e7a3343c92/8d013c56-22e6-4873-9d47-4672d2876b2f";

   // ** VARIABLE DETAILS **           ** WHAT ITS USED FOR IN THIS AZURE FUNCTION **
    string a2tPlate = null;         // The 'best' number plate value itself
    Int64 a2tEpoch = 0;             // The time of the alert, in miliseconds since Epoch (1/1/1970 - Unix milliseconds timestamp) note: int64 because its a big number, and we need to process it to a human friendly datetime
    string a2tCamId = null;         // Camera_id from OpenALPR
    string a2tCamName = null;       // Camera_name from OpenALPR
    string a2tCoordsLat = null;     // GPS Coords, is "-1" if no data from agent
    string a2tCoordsLon = null;     // Ditto
    string a2tConfidence = null;    // Percent confidence (decimal between 0 and 1) in a2tPlate
    string a2tPlateRegion = null;   // Region of best plate from OpenALPR (eg "au-qld" for Queensland, Australia)
    string a2tImage = null;         // base64 string representing thr JPEG image representing the 'best' from the images recorded
    string a2tMakeModel = null;     // Make and model of the car in photo (OpenALPR's best guess) We clean it up so "honda_jazz" becomes "HONDA JAZZ"
    string a2tMakeModelConfidence = null; // Percent confidence in a2tMakeModel
    string a2tAlertList = null;     // The name of the Alert list that this plate is on that caused the alert to fire
    string a2tAlertListId = null;   // The OpenANPR internal id for a2tList
    string a2tAlertDescription = null; // The description against this particular plate in the alert list
    string a2tConstructedViewUrl = null; // We build a URL that we can link the user to jump to the alert im OpenANPR 
    string a2tUuidReference = null; // OpenANPR UUID they use track each image/frame

    //  Read in the body we have been given (JSON Alert from OpenALPR) and store in requestBody
    string requestBody = new StreamReader(req.Body).ReadToEnd();

    //  Deserialise means to take the long string of JSON given to us and make it an object
    dynamic data = JsonConvert.DeserializeObject(requestBody);

    // Set variables up per the VARIABLE DETAILS above
    a2tPlate = data?.group.best_plate_number;
    a2tEpoch = Int64.Parse(data?.group.epoch_start.ToString());
    a2tCamId = data?.group.camera_id; 
    a2tCamName = data?.camera_name;
    a2tCoordsLat = data?.group.gps_latitude;
    a2tCoordsLon = data?.group.gps_longitude;
    a2tImage = data?.group.vehicle_crop_jpeg;
    a2tConfidence = data?.group.best_confidence;
    a2tUuidReference = data?.group.best_uuid;
    a2tPlateRegion = data?.group.best_region;
    a2tMakeModel = data?.group.vehicle.make_model.First.name.ToString().Replace("_"," ").ToUpper();
    a2tMakeModelConfidence= data?.group.vehicle.make_model.First.confidence;
    a2tAlertList = data?.alert_list;
    a2tAlertListId = data?.alert_list_id;
    a2tAlertDescription = data?.description;
    // Update this (noting the example of the search string for alertlist and plate) if not using Rekor's cloud service
    a2tConstructedViewUrl = $"https://cloud.openalpr.com/search/#search_type=alert&alertlist={a2tAlertListId}&plate_number={a2tPlate}";

    // Writes the alert list id and epoch string. Uncomment for debugging / tracing
    //log.LogInformation(a2tAlertListId);
    //log.LogInformation(a2tEpoch.ToString());

    // Here we are taking the epoch value (a2tEpoch) and using it to derive an English (Australia) datetime string
    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(a2tEpoch);
    string a2tDateTime = dateTimeOffset.DateTime.ToString("U", CultureInfo.CreateSpecificCulture("en-AU"));

    var httpClient = new HttpClient();

    // This is the MessageCard (note the O365 Connector service for this uses the MessageCard format
    // This is listed as "Office 365 Connector Cards" at the following card reference article
    // https://docs.microsoft.com/en-us/microsoftteams/platform/task-modules-and-cards/cards/cards-reference
    var body = @"{" + "\n" +
    @"	""@type"": ""MessageCard""," + "\n" +
    @"	""@context"": ""https://schema.org/extensions""," + "\n" +
    @"    ""summary"": ""Plate Notification""," + "\n" +
    $@"    ""title"": ""Plate {a2tPlate}""," + "\n" +
    $@"    ""text"": ""![Image of the number plate](data:image/png;base64,{a2tImage})""," + "\n" +
    @"    ""themeColor"": ""E81123""," + "\n" +
    @"	""sections"": [" + "\n" +
    @"		{" + "\n" +
    @"			""startGroup"": true," + "\n" +
    @"			""facts"": [" + "\n" +
    @"				{" + "\n" +
    @"					""name"": ""Date Recorded:""," + "\n" +
    $@"					""value"": ""{a2tDateTime}""" + "\n" +
    @"				}," + "\n" +
    @"				{" + "\n" +
    @"					""name"": ""Camera:""," + "\n" +
    $@"					""value"": ""{a2tCamName}""" + "\n" +
    @"				}," + "\n" +
    @"				{" + "\n" +
    @"					""name"": ""Alert List:""," + "\n" +
    $@"					""value"": ""{a2tAlertList}""" + "\n" +
    @"				},				{" + "\n" +
    @"					""name"": ""Alert Description:""," + "\n" +
    $@"					""value"": ""{a2tAlertDescription}""" + "\n" +
    @"				}," + "\n" +
    @"				{" + "\n" +
    @"					""name"": ""Link:""," + "\n" +
    $@"					""value"": ""[Open in OpenANPR]({a2tConstructedViewUrl})""" + "\n" +
    @"				}" + "\n" +
    @"			]" + "\n" +
    @"		}" + "\n" +
    @"	]" + "\n" +
    @"}";

    // Post that body to the webhookUrl
    await httpClient.PostAsync(webhookUrl, new StringContent(body));

    string responseMessage = string.IsNullOrEmpty(a2tPlate)
        ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Processed {a2tPlate}.";

            return new OkObjectResult(responseMessage);
}
