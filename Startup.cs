using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coravel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafetyQuizBot.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Trivial.Security;

namespace SafetyQuizBot
{
    public class Startup
    {
        public const int SHORT_PAUSE = 200;
        public const int LONG_PAUSE = 500;

        public const string Force_Wrong = "0ILm0KS#2j9CF*#pUGKK0YyHDs";
        public static Quiz quiz { get; set; }
        public static string[] filler = { "Alright. Let's see how you do with this question.", "Next question", "Okay.\nGoing to the next question", "Next question" };
        public static HashSignatureProvider sign { get; set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            string line;
            using (StreamReader sr = new StreamReader("Questions.json"))
            {
                // Read the stream to a string, and write the string to the console.
                line = sr.ReadToEnd();
            }
            sign = HashSignatureProvider.CreateHS512("upoOddMBrzPEoqlNk7EQrrw9Uqr_cK8Xpp-sI40HYTdflX8hBJsGynX_VLOyN8pAnwN9ILf5jFqz0pjf5YkBDRBwoOJg_O6arngYqgjPO0JSdIfh1GGn1s1UcCkT_rKIb06smL85rn7s9QjhlAN8uvrwm9rIxaMbsMqxZAwb6iNC8F00hqQ2AhDY1Jm48kHAyPUgXgSpahkHiy2six8JsnQknEGAJDNc0wDp2PPedfQUIu-qndpIOeixaesrg9KoATpaRlj1TTlg9ul_LOHbafQZ2Hq1qZHA-OgannyDLP0VTLbyHXIOiesuxvuBvDnQgdGirk96qPoBwWYkVZsW4g");
            quiz = Quiz.FromJson(line);
        }

        public enum BotContext
        {
            Welcome,
            Start,
            Finish,
            Quiz,
            Feedback,
            Ineligible,
            Email
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(3);
            });
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(5);
            });
            services.AddHttpContextAccessor();
            services.AddApplicationInsightsTelemetry();
            services.AddMvc();
            services.AddQueue();
        }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {

            }
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSession();
            app.UseStaticFiles();
            app.UseMvc();


        }
    }
}
