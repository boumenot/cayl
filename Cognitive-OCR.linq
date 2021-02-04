<Query Kind="Statements">
  <NuGetReference>Microsoft.Azure.CognitiveServices.Vision.ComputerVision</NuGetReference>
  <Namespace>Microsoft.Azure.CognitiveServices.Vision.ComputerVision</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models</Namespace>
</Query>

// Azure Cognitivie services are quite generous with their free tier, but you do need an
// Azure subscription.  The hardest part of this whole process was finding the link to
// click to create the endpoint to make the call.
//
// https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account?tabs=singleservice%2Cwindows
// https://ms.portal.azure.com/#create/Microsoft.CognitiveServicesComputerVision
//
// ## Coding Samples
//
// https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/quickstarts-sdk/client-library?tabs=visual-studio&pivots=programming-language-csharp
//
// ## NOTES
// I got much better results with Azure Cognitivie vs. tesseract with my *limited* testing.

string subscriptionKey = Util.GetPassword("AzureCognitive");
string endpoint = "https://me.cognitiveservices.azure.com/";

var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey)) { Endpoint = endpoint };
var result = await client.ReadInStreamAsync(File.OpenRead(@"img.png"));

// https://me.cognitiveservices.azure.com/vision/v3.1/read/analyzeResults/145504de-ae63-49eb-9f51-c1be7c394bda
var operationId = Guid.Parse(result.OperationLocation.Substring(result.OperationLocation.Length - 36));

ReadOperationResult ror;
do {
    ror = await client.GetReadResultAsync(operationId);
    Thread.Sleep(TimeSpan.FromSeconds(3));
} while (ror.Status == OperationStatusCodes.Running || ror.Status == OperationStatusCodes.NotStarted);

string.Join("\n", ror.AnalyzeResult.ReadResults
    .SelectMany(x => x.Lines)
    .Select(x => x.Text)
    .ToArray())
    .Dump();

