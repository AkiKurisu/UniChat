using Unity.Sentis;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
#if ADDRESSABLES_INSTALL
using UnityEngine.AddressableAssets;
#endif
namespace UniChat
{
    public abstract class ModelProvider
    {
        public abstract UniTask<Model> LoadModel(string path);
        
        public abstract UniTask<string> LoadTokenizer(string path);
        
        public const string UserDataProvider = "UserDataProvider";
        
        public const string StreamingAssetsProvider = "StreamingAssetsProvider";
        
        public const string ResourcesProvider = "ResourcesProvider";
        
        public const string AddressableProvider = "AddressableProvider";
    }
    
    public class ModelProviderFactory
    {
        public readonly Dictionary<string, Func<ModelProvider>> ProviderMap = new();
        
        private static ModelProviderFactory _instance;
        
        public static ModelProviderFactory Instance => _instance ??= new ModelProviderFactory();

        private ModelProviderFactory()
        {
            ProviderMap.Add(ModelProvider.UserDataProvider, () => new FileModelProvider());
            ProviderMap.Add(ModelProvider.StreamingAssetsProvider, () => new FileModelProvider() { FromStreamingAssets = true });
            ProviderMap.Add(ModelProvider.ResourcesProvider, () => new ResourcesModelProvider());
#if ADDRESSABLES_INSTALL
            ProviderMap.Add(ModelProvider.AddressableProvider, () => new AddressableModelProvider());
#endif
        }
        
        public ModelProvider Create(string providerType)
        {
            if (!ProviderMap.TryGetValue(providerType, out var provider))
            {
                throw new InvalidOperationException("Invalid provider type: " + providerType);
            }
            return provider();
        }
    }
    
    public class FileModelProvider : ModelProvider
    {
        /// <summary>
        /// Loading mode will be different in android platform when using stream assets path
        /// </summary>
        /// <value></value>
        public bool FromStreamingAssets { get; set; }
        
        public override async UniTask<Model> LoadModel(string path)
        {
            path = Path.Combine(PathUtil.ModelPath, path);
            if (FromStreamingAssets && Application.isMobilePlatform && !Application.isEditor)
            {
                using UnityWebRequest www = UnityWebRequest.Get(new Uri(path));
                await www.SendWebRequest().ToUniTask();
                byte[] data = www.downloadHandler.data;
                using var stream = new MemoryStream(data);
                return ModelLoader.Load(stream);
            }
            else
            {
                return ModelLoader.Load(path);
            }
        }
        
        public override async UniTask<string> LoadTokenizer(string path)
        {
            path = Path.Combine(PathUtil.ModelPath, path);
            if (FromStreamingAssets && Application.isMobilePlatform && !Application.isEditor)
            {
                using UnityWebRequest www = UnityWebRequest.Get(new Uri(path));
                await www.SendWebRequest().ToUniTask();
                return www.downloadHandler.text;
            }

            return await File.ReadAllTextAsync(path).AsUniTask();
        }
    }
    
    public class ResourcesModelProvider : ModelProvider
    {
        public override async UniTask<Model> LoadModel(string path)
        {
            return ModelLoader.Load((ModelAsset)await Resources.LoadAsync<ModelAsset>(path).ToUniTask());
        }

        public override async UniTask<string> LoadTokenizer(string path)
        {
            return ((TextAsset)await Resources.LoadAsync<TextAsset>(path).ToUniTask()).text;
        }
    }
    
#if ADDRESSABLES_INSTALL
    public class AddressableModelProvider : ModelProvider
    {
        public override async UniTask<Model> LoadModel(string path)
        {
            var handle = Addressables.LoadAssetAsync<ModelAsset>(path);
            var model = ModelLoader.Load(await handle.ToUniTask());
            Addressables.Release(handle);
            return model;
        }

        public override async UniTask<string> LoadTokenizer(string path)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(path);
            var tokenizerJson = (await handle.ToUniTask()).text;
            Addressables.Release(handle);
            return tokenizerJson;
        }
    }
#endif
}