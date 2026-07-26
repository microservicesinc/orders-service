using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Cdk
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            
            // Instantiates your specific CdkStack definition
            new CdkStack(app, "OrdersCdkStack", new StackProps
            {
                // Optional: Customize your target deployment environment here
                Env = new Amazon.CDK.Environment
                {
                    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
                }
            });

            app.Synth();
        }
    }
}
