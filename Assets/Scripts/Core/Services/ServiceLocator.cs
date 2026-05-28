using System;
using System.Collections.Generic;
using UnityEngine;
using InteractiveNPC.Core.Utilities;

namespace InteractiveNPC.Core.Services
{
    #region Service Locator Interface
    
    /// <summary>
    /// Interface for services that can be registered with the ServiceLocator.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Initialize the service. Called when the service is first registered.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Cleanup the service. Called when the service is unregistered or the application quits.
        /// </summary>
        void Shutdown();
    }
    
    #endregion
    
    #region Service Locator Implementation
    
    /// <summary>
    /// Lightweight service locator for managing singleton services.
    /// Provides dependency injection without tight coupling between systems.
    /// </summary>
    public static class ServiceLocator
    {
        #region Private Fields
        
        private static readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();
        private static readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private static bool _isInitialized;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the service locator. Called automatically on application start.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _isInitialized = true;
            Application.quitting += OnApplicationQuit;
            DebugLogger.LogInfo("ServiceLocator initialized", LogCategory.General);
        }
        
        /// <summary>
        /// Cleanup all services when the application quits.
        /// </summary>
        private static void OnApplicationQuit()
        {
            ShutdownAllServices();
            _isInitialized = false;
        }
        
        #endregion
        
        #region Service Registration
        
        /// <summary>
        /// Register a service with the locator.
        /// </summary>
        /// <typeparam name="T">The interface type to register the service as.</typeparam>
        /// <param name="service">The service instance to register.</param>
        /// <param name="initializeImmediately">Whether to call Initialize() on the service immediately.</param>
        public static void Register<T>(T service, bool initializeImmediately = true) where T : class, IService
        {
            Type type = typeof(T);
            
            if (_services.ContainsKey(type))
            {
                DebugLogger.LogWarning($"Service of type {type.Name} is already registered. Replacing...", LogCategory.General);
                Unregister<T>();
            }
            
            _services[type] = service;
            
            if (initializeImmediately)
            {
                try
                {
                    service.Initialize();
                    DebugLogger.LogInfo($"Service {type.Name} registered and initialized", LogCategory.General);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException(ex, LogCategory.General);
                    _services.Remove(type);
                    throw;
                }
            }
            else
            {
                DebugLogger.LogInfo($"Service {type.Name} registered (not initialized)", LogCategory.General);
            }
        }
        
        /// <summary>
        /// Register a non-IService instance for dependency injection.
        /// </summary>
        /// <typeparam name="T">The type to register the instance as.</typeparam>
        /// <param name="instance">The instance to register.</param>
        public static void RegisterInstance<T>(T instance) where T : class
        {
            Type type = typeof(T);
            
            if (_instances.ContainsKey(type))
            {
                DebugLogger.LogWarning($"Instance of type {type.Name} is already registered. Replacing...", LogCategory.General);
            }
            
            _instances[type] = instance;
            DebugLogger.LogInfo($"Instance {type.Name} registered", LogCategory.General);
        }
        
        /// <summary>
        /// Unregister a service from the locator.
        /// </summary>
        /// <typeparam name="T">The interface type of the service to unregister.</typeparam>
        public static void Unregister<T>() where T : class, IService
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out IService service))
            {
                try
                {
                    service.Shutdown();
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException(ex, LogCategory.General);
                }
                
                _services.Remove(type);
                DebugLogger.LogInfo($"Service {type.Name} unregistered", LogCategory.General);
            }
        }
        
        /// <summary>
        /// Unregister an instance from the locator.
        /// </summary>
        /// <typeparam name="T">The type of the instance to unregister.</typeparam>
        public static void UnregisterInstance<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_instances.ContainsKey(type))
            {
                _instances.Remove(type);
                DebugLogger.LogInfo($"Instance {type.Name} unregistered", LogCategory.General);
            }
        }
        
        #endregion
        
        #region Service Retrieval
        
        /// <summary>
        /// Get a registered service by type.
        /// </summary>
        /// <typeparam name="T">The interface type of the service to retrieve.</typeparam>
        /// <returns>The registered service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no service of the specified type is registered.</exception>
        public static T Get<T>() where T : class, IService
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out IService service))
            {
                return service as T;
            }
            
            throw new InvalidOperationException($"Service of type {type.Name} is not registered. " +
                "Make sure to register the service before requesting it.");
        }
        
        /// <summary>
        /// Try to get a registered service by type.
        /// </summary>
        /// <typeparam name="T">The interface type of the service to retrieve.</typeparam>
        /// <param name="service">The retrieved service instance, or null if not found.</param>
        /// <returns>True if the service was found, false otherwise.</returns>
        public static bool TryGet<T>(out T service) where T : class, IService
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out IService registeredService))
            {
                service = registeredService as T;
                return service != null;
            }
            
            service = null;
            return false;
        }
        
        /// <summary>
        /// Get a registered instance by type.
        /// </summary>
        /// <typeparam name="T">The type of the instance to retrieve.</typeparam>
        /// <returns>The registered instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no instance of the specified type is registered.</exception>
        public static T GetInstance<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_instances.TryGetValue(type, out object instance))
            {
                return instance as T;
            }
            
            throw new InvalidOperationException($"Instance of type {type.Name} is not registered.");
        }
        
        /// <summary>
        /// Try to get a registered instance by type.
        /// </summary>
        /// <typeparam name="T">The type of the instance to retrieve.</typeparam>
        /// <param name="instance">The retrieved instance, or null if not found.</param>
        /// <returns>True if the instance was found, false otherwise.</returns>
        public static bool TryGetInstance<T>(out T instance) where T : class
        {
            Type type = typeof(T);
            
            if (_instances.TryGetValue(type, out object registeredInstance))
            {
                instance = registeredInstance as T;
                return instance != null;
            }
            
            instance = null;
            return false;
        }
        
        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        /// <typeparam name="T">The interface type of the service to check.</typeparam>
        /// <returns>True if the service is registered, false otherwise.</returns>
        public static bool IsRegistered<T>() where T : class, IService
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Check if an instance is registered.
        /// </summary>
        /// <typeparam name="T">The type of the instance to check.</typeparam>
        /// <returns>True if the instance is registered, false otherwise.</returns>
        public static bool IsInstanceRegistered<T>() where T : class
        {
            return _instances.ContainsKey(typeof(T));
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Shutdown all registered services.
        /// </summary>
        public static void ShutdownAllServices()
        {
            foreach (var kvp in _services)
            {
                try
                {
                    kvp.Value.Shutdown();
                    DebugLogger.LogInfo($"Service {kvp.Key.Name} shutdown", LogCategory.General);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException(ex, LogCategory.General);
                }
            }
            
            _services.Clear();
            _instances.Clear();
            DebugLogger.LogInfo("All services shutdown and cleared", LogCategory.General);
        }
        
        /// <summary>
        /// Get the count of registered services.
        /// </summary>
        public static int ServiceCount => _services.Count;
        
        /// <summary>
        /// Get the count of registered instances.
        /// </summary>
        public static int InstanceCount => _instances.Count;
        
        #endregion
    }
    
    #endregion
    
    #region Service Base Classes
    
    /// <summary>
    /// Abstract base class for services that require MonoBehaviour functionality.
    /// Automatically registers itself with the ServiceLocator on Awake.
    /// </summary>
    /// <typeparam name="T">The interface type this service implements.</typeparam>
    public abstract class MonoBehaviourService<T> : MonoBehaviour, IService where T : class, IService
    {
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            if (!ServiceLocator.IsRegistered<T>())
            {
                ServiceLocator.Register(this as T, false);
                Initialize();
            }
            else
            {
                DebugLogger.LogWarning($"Service {typeof(T).Name} already exists. Destroying duplicate.", LogCategory.General);
                Destroy(gameObject);
            }
        }
        
        protected virtual void OnDestroy()
        {
            if (ServiceLocator.TryGet<T>(out T service) && ReferenceEquals(service, this))
            {
                Shutdown();
                ServiceLocator.Unregister<T>();
            }
        }
        
        #endregion
        
        #region IService Implementation
        
        public virtual void Initialize()
        {
            DebugLogger.LogInfo($"{typeof(T).Name} initialized", LogCategory.General);
        }
        
        public virtual void Shutdown()
        {
            DebugLogger.LogInfo($"{typeof(T).Name} shutdown", LogCategory.General);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Abstract base class for services that persist across scenes.
    /// </summary>
    /// <typeparam name="T">The interface type this service implements.</typeparam>
    public abstract class PersistentMonoBehaviourService<T> : MonoBehaviourService<T> where T : class, IService
    {
        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }
    }
    
    #endregion
}
