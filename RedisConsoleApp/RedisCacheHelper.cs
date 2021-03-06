﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Redis;
using System.Configuration;


namespace RedisConsoleApp
{

  public  class RedisCacheHelper
    {
        private static readonly PooledRedisClientManager pool = null;
        private static readonly string[] writeHostos = null;
        private static readonly string[] readHostos = null;
        public static int RedisMaxReadPool = int.Parse(ConfigurationManager.AppSettings["redis_max_read_pool"]);
        public static int RedisMaxWritePool = int.Parse(ConfigurationManager.AppSettings["redis_max_write_pool"]);
        static RedisCacheHelper()
        {   


            
            var redisMasterHost = ConfigurationManager.AppSettings["redis_server_master_session"];
            var redisSlaveHost = ConfigurationManager.AppSettings["redis_server_slave_session"];

            if (!string.IsNullOrEmpty(redisMasterHost))
            {
                writeHostos = redisMasterHost.Split(',');
                readHostos = redisSlaveHost.Split(',');

                if (readHostos.Length > 0)
                {
                    pool = new PooledRedisClientManager(writeHostos, readHostos,
                        new RedisClientManagerConfig()
                        {
                            MaxWritePoolSize = RedisMaxWritePool,
                            MaxReadPoolSize = RedisMaxReadPool,

                            AutoStart = true
                        });
                }
            }
        }
        public static void Add<T>(string key, T value, DateTime expiry)
        {
            if (value == null)
            {
                return;
            }

            if (expiry <= DateTime.Now)
            {
                Remove(key);

                return;
            }

            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            r.Set(key, value, expiry - DateTime.Now);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "存储", key);
            }

        }

        public static void Add<T>(string key, T value, TimeSpan slidingExpiration)
        {
            if (value == null)
            {
                return;
            }

            if (slidingExpiration.TotalSeconds <= 0)
            {
                Remove(key);

                return;
            }

            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            r.Set(key, value, slidingExpiration);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "存储", key);
            }

        }

        public static T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return default(T);
            }

            T obj = default(T);

            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            obj = r.Get<T>(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "获取", key);
            }


            return obj;
        }

        public static void Remove(string key)
        {
            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            r.Remove(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "删除", key);
            }

        }

        public static bool Exists(string key)
        {
            try
            {
                if (pool != null)
                {
                    using (var r = pool.GetClient())
                    {
                        if (r != null)
                        {
                            r.SendTimeout = 1000;
                            return r.ContainsKey(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "是否存在", key);
            }

            return false;
        }
        public static string str(string str)
        {
            return str.Substring(0, 2);
        }
        public static IDictionary<string, T> GetAll<T>(IEnumerable<string> keys) where T : class
        {
            if (keys == null)
            {
                return null;
            }

            keys = keys.Where(k => !string.IsNullOrWhiteSpace(k));

            if (keys.Count() == 1)
            {
                T obj = Get<T>(keys.Single());

                if (obj != null)
                {
                    return new Dictionary<string, T>() { { keys.Single(), obj } };
                }


                
                
                return null;
            }

            if (!keys.Any())
            {
                return null;
            }

            IDictionary<string, T> dict = null;


            //var key=keys.Select(t => 
            //{
            //    try
            //    {
            //        if (t != null)
            //        {
            //            return str(t);
            //        }
            //    }
            //    catch (Exception)
            //    {

            //        throw;
            //    }
            //    return null;
            //});




            //if (pool != null)
            //{
            //    keys.Select(s => new
            //    {
            //        Index = Math.Abs(s.GetHashCode()) % readHostos.Length,
            //        KeyName = s
            //    })
            //    .GroupBy(p => p.Index)
            //    .Select(g =>
            //    {
            //        try
            //        {
            //            using (var r = pool.GetClient())
            //            {
            //                if (r != null)
            //                {
            //                    r.SendTimeout = 1000;
            //                    return r.GetAll<T>(g.Select(p => p.KeyName));
            //                }
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            string msg = string.Format("{0}:{1}发生异常!{2}", "cache", "获取", keys.Aggregate((a, b) => a + "," + b));
            //        }
            //        return null;
            //    })
            //    .Where(x => x != null)
            //    .ForEach(d =>
            //    {
            //        d.ForEach(x =>
            //        {
            //            if (dict == null || !dict.Keys.Contains(x.Key))
            //            {
            //                if (dict == null)
            //                {
            //                    dict = new Dictionary<string, T>();
            //                }
            //                dict.Add(x);
            //            }
            //        });
            //    });
            //}

            IEnumerable<Tuple<string, T>> result = null;

            if (dict != null)
            {
                result = dict.Select(d => new Tuple<string, T>(d.Key, d.Value));
            }
            else
            {
                result = keys.Select(key => new Tuple<string, T>(key, Get<T>(key)));
            }

            return result
                .Select(d => new Tuple<string[], T>(d.Item1.Split('_'), d.Item2))
                .Where(d => d.Item1.Length >= 2)
                .ToDictionary(x => x.Item1[1], x => x.Item2);
        }

    }
}
