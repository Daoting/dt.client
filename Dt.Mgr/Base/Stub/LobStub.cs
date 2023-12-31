﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2016-02-18
******************************************************************************/
#endregion

#region 引用命名
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
#endregion

namespace Dt.Mgr
{
    /// <summary>
    /// 标准服务存根
    /// </summary>
    public partial class LobStub : DefaultStub
    {
        /// <summary>
        /// 设置cm服务地址，末尾无/，如：https://10.10.1.16/dt-cm
        /// <para>请在构造方法中设置</para>
        /// </summary>
        public string SvcUrl
        {
            get { return (Kit.GetRequiredService<IRpcConfig>() as LobRpcConfig).SvcUrl; }
            protected set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    (Kit.GetRequiredService<IRpcConfig>() as LobRpcConfig).SvcUrl = value.TrimEnd('/');
            }
        }

        /// <summary>
        /// 获取可选的cm服务地址列表，用于切换服务时选择，切换服务也支持手动填写
        /// </summary>
        public List<string> SvcUrlOptions
        {
            get { return (Kit.GetRequiredService<IRpcConfig>() as LobRpcConfig).SvcUrlOptions; }
            protected set
            {
                (Kit.GetRequiredService<IRpcConfig>() as LobRpcConfig).SvcUrlOptions = value;
            }
        }

        /// <summary>
        /// 注入全局服务
        /// </summary>
        /// <param name="p_svcs"></param>
        protected override void ConfigureServices(IServiceCollection p_svcs)
        {
            base.ConfigureServices(p_svcs);
            p_svcs.AddSingleton<IRpcConfig, LobRpcConfig>();
            p_svcs.AddSingleton<IModelCallback, ModelCallback>();
            p_svcs.AddSingleton<IUserCallback, UserCallback>();
        }

        /// <summary>
        /// 连接cm服务，获取全局参数，更新打开模型库
        /// </summary>
        /// <returns></returns>
        protected sealed override async Task InitConfig()
        {
            await (Kit.GetRequiredService<IRpcConfig>() as LobRpcConfig).LoadCustomSvcUrl();

            // 获取全局参数：服务器时间、所有服务地址、模型文件版本号
            Dict cfg;
            try
            {
                cfg = await AtCm.GetConfig();
            }
            catch
            {
                throw new Exception("服务器连接失败！");
            }

            if (cfg == null)
                throw new Exception("获取参数失败！");

            // 服务器时间
            if (!cfg.ContainsKey("Now"))
                throw new Exception("获取服务器时间失败！");
            Kit.SyncTime(cfg.Date("Now"));

            // 初始化服务地址
            if (!cfg.ContainsKey("IsSingletonSvc"))
                throw new Exception("获取服务地址失败！");
            bool isSingleton = cfg.Bool("IsSingletonSvc");
            if (!isSingleton && !cfg.ContainsKey("SvcUrls"))
                throw new Exception("未包含所有服务地址！服务端缺少url.json文件");
            string svcName = cfg.ContainsKey("EntitySvcName") ? cfg.Str("EntitySvcName") : "cm";
            Kit.SetOriginSvcName(svcName);
            var rpc = Kit.GetRequiredService<IRpcConfig>() as LobRpcConfig;
            rpc.InitSvcUrls(isSingleton ? null : (Dict)cfg["SvcUrls"]);

            // 更新sqlite文件
            if (!cfg.ContainsKey("SqliteVer"))
                throw new Exception("缺少sqlite文件版本号！服务端先根据sqlite.json创建这些文件");
            List<string> ls = cfg.StrList("SqliteVer");
            foreach (var ver in ls)
            {
                await UpdateSqliteFile(ver);
            }
        }

        /// <summary>
        /// 更新sqlite文件，与本地不同时下载新文件；
        /// </summary>
        /// <param name="p_ver"></param>
        /// <returns></returns>
        async Task UpdateSqliteFile(string p_ver)
        {
            // 版本号文件，空文件，标志用
            string verFile = Path.Combine(Kit.DataPath, $"{p_ver}.ver");
            if (File.Exists(verFile))
                return;

            string name = p_ver.Split('_')[0];
            string dbFile = Path.Combine(Kit.DataPath, name + ".db");

            // 删除旧版的db文件和版本号文件
            try { File.Delete(dbFile); } catch { }
            foreach (var file in new DirectoryInfo(Kit.DataPath).GetFiles($"{name}_*.ver"))
            {
                try { file.Delete(); } catch { }
            }

            try
            {
                // 下载模型文件，下载地址如 https://localhost/app-cm/.sqlite/model
                using (var response = await Kit.RpcClient.GetAsync($"{Kit.GetSvcUrl("cm")}/.sqlite/{name}"))
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                using (var fs = File.Create(dbFile, 262140, FileOptions.WriteThrough))
                {
                    gzipStream.CopyTo(fs);
                    fs.Flush();
                }

                // 空版本号文件
                File.Create(verFile);
            }
            catch (Exception ex)
            {
                try
                {
                    File.Delete(dbFile);
                }
                catch { }
                throw new Exception($"下载 {p_ver}.gz 文件失败！" + ex.Message);
            }
        }
    }
}