using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace SimpleCalculator
{	
	//MEF 中的组合是递归的。 你明确撰写了 Program 对象（导入了结果为 ICalculator 类型的 MySimpleCalculator）。 反过来，MySimpleCalculator 导入一组 IOperation 对象，且该导入将在创建 MySimpleCalculator 时进行填写，同时进行 Program 的导入。

    class Program
    {
		//MEF组合模型的核心是包含所有可用不见并执行组合的组合容器，组合是对导入到导出进行的匹配
		//声明类Hosting.CompositionContainer（管理部件的组合）的私有字段，用来存储目录、目录用来发现可用部件的对象
        private CompositionContainer _container;
		//Program的构造函数
        private Program()
        {
            try
            {
                // 组合多个目录的聚合目录
                var catalog = new AggregateCatalog();
                // 把所有与Program类相同的部件添加进目录
                catalog.Catalogs.Add(new AssemblyCatalog(typeof(Program).Assembly));
                catalog.Catalogs.Add(new DirectoryCatalog("C:\\Users\\COscillator\\source\\repos\\SimpleCalculator\\SimpleCalculator\\Extensions"));
                // 使用目录中的部件创建CompositionContainer类的字段_container
                _container = new CompositionContainer(catalog);
                _container.ComposeParts(this);
            }
			//捕获CompositionContainer部件进行组合期间发生的一个或多个异常并输出
            catch (CompositionException compositionException)
            {
                Console.WriteLine(compositionException.ToString());
            }

        }
		
	
	    
		//calculator对象使用了ImportAttribute属性进行修饰，声明了其作为导入
		//每个导入都应该有一个决定其与什么导出相匹配的协定，下述即协定为公用接口ICalculator（MEF自动假定协定基于导入的类型）
        [Import(typeof(ICalculator))]
        public ICalculator calculator;
    
	    public interface ICalculator
       {
        string Calculate(string input);
       }
	   //定义执行ICalculator的类
	    [Export(typeof(ICalculator))]
    class MySimpleCalculator : ICalculator
    {
		//创建可由任意数目的导出填写的导入
        [ImportMany]
		//Lazy<T,TMetadata>是由MEF提供来保存对导出的间接引用的类型，除了导出对像本身，还可以获取导出元数据，或描述导出对象的信息。每个Lazy<T,TMetadata>都包含一个代表实际操作的IOperation对象和一个代表元数据的IOperationData对象
        IEnumerable<Lazy<IOperation, IOperationData>> operations;
        
		//计算器逻辑
		public String Calculate(string input)
        {
            int left;
            int right;
            char operation;
            // Finds the operator.
            int fn = FindFirstNonDigit(input);
			//fn是屏幕输入中第一个非十进制数字所在的位置，fn<0即返回-1的情况，表示已经检测完整个屏幕输入的字符串（用于后台计算时、表明程序仍在运行）
            if (fn < 0) return "Could not parse command.";

            try
            {
                // 分离操作符，将输入的字符串在fn前的作为left，在fn后的作为right
                left = int.Parse(input.Substring(0, fn));
                right = int.Parse(input.Substring(fn + 1));
            }
            catch
            {
                return "Could not parse command.";
            }
			
			//Unicode类型的字段operation声明为input[]索引器中第fn个（即运算符）
            operation = input[fn];
			
			//遍历部件组合，查找枚举operations的每个成员（Lazy<T,TMetadata>类型）
            foreach (Lazy<IOperation, IOperationData> i in operations)
            {
				//当operations的成员Symbol和operation属性匹配时，即屏幕输入的运算符与任意导入的部件Symbol属性相同，执行该部件的Operate方法，并转换为String类型
                if (i.Metadata.Symbol.Equals(operation))
                {
                    return i.Value.Operate(left, right).ToString();
                }
            }
			//如果遍历部件组合，没有找到部件成员Symbol和运算符相同的，则返回未找到该运算
            return "Operation Not Found!";
        }
		
		//声明返回类型为int、参数类型为string的私有方法FindFirstNonDigit，用来检查输入的字符串s，当检测到十进制字符时返回-1，遇到第一个非十进制字符则返回i值
        private int FindFirstNonDigit(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (!char.IsDigit(s[i])) return i;
            }
            return -1;
        }
    }
		//声明IOperation对象
	    public interface IOperation
       {
        int Operate(int left, int right);
       }
		
		//声明IOperation元数据
    	public interface IOperationData
       {
        char Symbol { get; }
       }
		
		//ExportAttribute属性函数和之前的导入IOperation一致
		[Export(typeof(IOperation))]
		//ExportMetadataAttribute属性采用名称值对形式的元数据附加到输出
		[ExportMetadata("Symbol", '+')]
		//派生类Add执行父类IOperation，但并没有对其中的方法明确定义，相反由MEF隐式创建的类具有基于提供的元数据名称的属性
		class Add : IOperation
		{
			public int Operate(int left, int right)
			{
            return left + right;
			}
		}

		[Export(typeof(IOperation))]
		[ExportMetadata("Symbol", '-')]
		class Subtract : IOperation
		{
			public int Operate(int left, int right)
			{
            return left - right;
			}
		}

        static void Main(string[] args)
        {
            // 在构造函数中进行容器组合
            var p = new Program();
            Console.WriteLine("Enter Command:");
            while (true)
            {
                string s = Console.ReadLine();
                Console.WriteLine(p.calculator.Calculate(s));
            }
        }
    }
}
