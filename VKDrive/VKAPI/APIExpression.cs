using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive.VKAPI
{
	internal class ApiExpression : IApiQuery
	{
		private readonly string _expression;
        public ApiExpression(string expression)
        {
	        _expression = expression;
        }

		public override string ToString()
		{
			return _expression;
		}
	}
}
