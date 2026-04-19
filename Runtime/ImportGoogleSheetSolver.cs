using Knot.Localization.Data;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Knot.Localization
{
    [Serializable]
    public sealed class ImportGoogleSheetSolver : KnotLocalizationImportExport.ImportExportSolver
    {
        // 16Uiu4od18zgB4lrKQUXBdPOPOzpEreFzTrvABI0aNXY

        [SerializeField] private string fileId;
        [SerializeField] private int pageId;

        public override void Import(KnotDatabase database)
        {
            #if UNITY_EDITOR
            if (!Enabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(fileId)
                || Regex.IsMatch(fileId, @"[^\w\d-_]+",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                KnotLocalization.Log($"Incorrect {nameof(fileId)} param: {fileId}", LogType.Error);
                return;
            }

            if (database.TextKeyCollections == null
                || !database.TextKeyCollections.Any(collection => (bool)collection))
            {
                KnotLocalization.Log("Text Key Collections doesn't set in database!", LogType.Error);
                return;
            }

            ImportInternal(database);
            #endif
        }

        public override void Export(KnotDatabase database)
        {
            #if UNITY_EDITOR
            if (!Enabled || string.IsNullOrEmpty(fileId))
            {
                return;
            }

            KnotLocalization.Log("Export to google sheets not implemented.", LogType.Warning);
            #endif
        }

        private void ImportInternal(KnotDatabase database)
        {
            KnotLocalization.Log("Google import started...", LogType.Log);

            var webRequest = BuildWebRequest(fileId, pageId);
            var requestHandler = webRequest.SendWebRequest();

            requestHandler.completed += OnRequestHandlerCompleted;

            return;

            void OnRequestHandlerCompleted(AsyncOperation asyncOperation)
            {
                var op = (UnityWebRequestAsyncOperation)asyncOperation;
                if (op.webRequest.result != UnityWebRequest.Result.Success)
                {
                    KnotLocalization.Log(op.webRequest.error, LogType.Warning);
                    return;
                }

                var handler = op.webRequest.downloadHandler;
                var separator = Environment.NewLine.ToCharArray();
                var strings = handler.text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                var importLanguages = new KnotLanguageData[strings.Length];

                var updatedKeys = 0;
                var newKeys = 0;
                for (var i = 0; i < strings.Length; i++)
                {
                    var values = strings[i].Split('\t');
                    var key = values[0];
                    var isKeyUpdated = false;

                    // parse languages
                    if (i == 0)
                    {
                        for (var k = 1; k < values.Length; k++)
                        {
                            importLanguages[k] = null;

                            var langRaw = values[k].Trim();
                            var lang = langRaw.ToLower();
                            var languageData = database.Languages.FirstOrDefault(data => lang.EndsWith($"[{data.CultureName.ToLower()}]"));

                            if (languageData == null)
                            {
                                KnotLocalization.Log($"Skip {langRaw}: not found language in database.", LogType.Warning);
                                continue;
                            }

                            if (languageData.CollectionProviders == null)
                            {
                                KnotLocalization.Log($"Skip {langRaw}: provider doesn't set.", LogType.Warning);
                                continue;
                            }

                            var hasProvider = false;
                            foreach (var provider in languageData.CollectionProviders)
                            {
                                if (provider is IKnotPersistentItemCollectionProvider { Collection: IKnotItemCollection<KnotTextData> })
                                {
                                    hasProvider = true;
                                    break;
                                }
                            }

                            if (!hasProvider)
                            {
                                KnotLocalization.Log($"Skip {langRaw}: not found provider or provider collection is null.", LogType.Warning);
                                continue;
                            }

                            importLanguages[k] = languageData;
                        }

                        continue;
                    }

                    // TODO manual setup collection for import?

                    KnotKeyData keyData = null;
                    KnotKeyCollection firstKeyCollection = null;
                    foreach (var keyCollection in database.TextKeyCollections)
                    {
                        if (keyCollection is null)
                        {
                            continue;
                        }

                        firstKeyCollection ??= keyCollection;

                        keyData = keyCollection.FirstOrDefault(data => data.Key == key);
                        if (keyData != null)
                        {
                            break;
                        }
                    }

                    // add key if doesn't exist
                    if (keyData == null && firstKeyCollection != null)
                    {
                        firstKeyCollection.Add(new KnotKeyData(key));
                        EditorUtility.SetDirty(firstKeyCollection);
                        newKeys++;
                    }

                    for (var j = 1; j < values.Length; j++)
                    {
                        var targetLanguage = importLanguages[j];
                        if (targetLanguage == null)
                        {
                            continue;
                        }

                        var rawText = values[j].Trim();
                        var isKeyFound = false;
                        IKnotPersistentItemCollectionProvider targetProvider = null;
                        foreach (var candidateProvider in targetLanguage.CollectionProviders)
                        {
                            if (candidateProvider is not IKnotPersistentItemCollectionProvider
                                {
                                    Collection: IKnotItemCollection<KnotTextData> textCollection
                                } provider)
                            {
                                continue;
                            }

                            targetProvider ??= provider;

                            var textData = textCollection.FirstOrDefault(data => data.Key == key);
                            if (textData is null)
                            {
                                continue;
                            }

                            isKeyFound = true;
                            if (textData.RawText == rawText)
                            {
                                break;
                            }

                            textData.RawText = rawText;
                            isKeyUpdated = true;
                            EditorUtility.SetDirty(provider.Collection);
                            break;
                        }

                        if (isKeyFound)
                        {
                            continue;
                        }

                        if (targetProvider?.Collection is IKnotItemCollection<KnotTextData> collection)
                        {
                            collection.Add(new KnotTextData(key, rawText));
                            isKeyUpdated = true;
                            EditorUtility.SetDirty(targetProvider.Collection);
                        }
                    }

                    if (isKeyUpdated)
                    {
                        updatedKeys++;
                    }
                }

                KnotLocalization.Log($"New keys: {newKeys}", LogType.Log);
                KnotLocalization.Log($"Keys updated: {updatedKeys}", LogType.Log);
                KnotLocalization.Log("Google import finished!", LogType.Log);
            }
        }

        private static UnityWebRequest BuildWebRequest(string fileId, int pageId)
            => UnityWebRequest.Get($"https://docs.google.com/spreadsheets/d/{fileId}/export?format=tsv&gid={pageId}");
    }
}