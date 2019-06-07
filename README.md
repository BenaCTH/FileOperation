# FileOperation
大文件拆分上传，下载

中心思想：
前台利用 file.slice() 将文件按照指定大小拆分
data.append("file", file.slice(start, end), name);

在后台接收拆分的文件，

download 时 当文件超出指定大小后按照HttpWebRequest 的当时请求文件。

# Flex 实现多个div 等分
      .parent {
            display: flex;
        }

        .child {
            flex: 1 1 0;
            height: 30px;
            overflow:hidden;
            white-space:nowrap;
        }

            .child + .child {
                margin-left: 10px;
            }
           <div class="parent" style="background-color: lightgrey;">
                <div class="child" style="background-color: lightblue;">1sdadawsdfasfasdfeqw asdasdasdweqe dadsa dad as</div>
                <div class="child" style="background-color: lightblue;">2</div>
                <div class="child" style="background-color: lightblue;">3</div>
                <div class="child" style="background-color: lightblue;">4</div>
            </div>
            <input type="button" value="Add" id="add" />
            
          
# IE 浏览器不解析字体图标解决办法

        if (url.Contains("resources"))
        {
            context.Response.GetTypedHeaders().CacheControl =
                new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                {
                    Public = true
                };
        }
                    

 
       public static IApplicationBuilder UseCommonConfigure(this IApplicationBuilder app, CommonConstants.Module module)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
            });

            app.UseCommonMiddleware();
            var env = Environment.GetEnvironmentVariable(Constants.ASPNETCORE_ENVIRONMENT);
            string deployHost = Environment.GetEnvironmentVariable(CommonConstants.AppSettingServiceName.ConfigDeployHostName);
            logger.Debug($"ENVIRONMENT:{env}, deployHost:{deployHost}");
            if (string.Equals(env, Constants.DEV_ENVIRONMENT, StringComparison.OrdinalIgnoreCase))
            {
                app.Use(async (context, next) =>
                {
                    var url = context.Request.Path.ToString();

                    //request resources files
                    if (url.Contains("resources"))
                    {
                        context.Response.GetTypedHeaders().CacheControl =
                            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                            {
                                Public = true
                            };
                    }
                    else //other request, api and so on
                    {
                        context.Response.GetTypedHeaders().CacheControl =
                            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                            {
                                NoStore = true,
                                NoCache = true,
                                //Public = true,
                                MustRevalidate = true
                            };
                        context.Response.Headers["Pragma"] = "no-cache";
                        context.Response.Headers["Expires"] = "-1";
                    }
                    context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
                        new string[] { "Accept-Encoding" };
                    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                    context.Response.Headers["X-XSS-Protection"] = "1;mode=block";
                    //context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["Strict-Transport-Security"] = "max-age=16070400;includeSubDomains";
                    //context.Response.Headers[""]

                    await next();
                });
            }
            else
            {
                app.Use(async (context, next) =>
                {
                    var url = context.Request.Path.ToString();

                    //request resources files
                    if (url.Contains("resources"))
                    {
                        context.Response.GetTypedHeaders().CacheControl =
                            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                            {
                                Public = true
                            };
                    }
                    else //other request, api and so on
                    {
                        context.Response.GetTypedHeaders().CacheControl =
                            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                            {
                                NoStore = true,
                                NoCache = true,
                                //Public = true,
                                MustRevalidate = true
                            };
                        context.Response.Headers["Pragma"] = "no-cache";
                        context.Response.Headers["Expires"] = "-1";
                    }
                    context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
                        new string[] { "Accept-Encoding" };
                    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                    context.Response.Headers["X-XSS-Protection"] = "1;mode=block";
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["Content-Security-Policy"] = $"script-src 'unsafe-inline' {deployHost}; base-uri 'none'; object-src 'none';";
                    context.Response.Headers["Strict-Transport-Security"] = "max-age=16070400;includeSubDomains";
                    //context.Response.Headers[""]

                    await next();
                });
            }
            app.UseCors("CorsPolicy");
            app.UseStaticFiles();
            app.UseCustomAuthentication();
            app.UseCommonWorkflow(module);
            app.UseMvc();
            IdentityModelEventSource.ShowPII = true;

            return app;
        }
