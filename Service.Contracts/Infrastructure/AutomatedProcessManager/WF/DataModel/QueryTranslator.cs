using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	public class TranslatedQuery
	{
		public string QueryExpression;
		public List<object> Arguments;
	}

	public class QueryTranslator : ExpressionVisitor
	{
		private StringBuilder sb;
		private object inputObject;
		private List<object> args;

		public QueryTranslator()
		{
		}

		public TranslatedQuery Translate(Expression expression, object input)
		{
			sb = new StringBuilder(1000);
			inputObject = input;
			args = new List<object>();

			Visit(expression);

			return new TranslatedQuery()
			{
				QueryExpression = sb.ToString(),
				Arguments = args
			};
		}

		protected override Expression VisitMethodCall(MethodCallExpression m)
		{
			throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
		}

		protected override Expression VisitUnary(UnaryExpression u)
		{
			switch (u.NodeType)
			{
				case ExpressionType.Not:
					sb.Append(" NOT ");
					this.Visit(u.Operand);
					break;
				default:
					throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
			}
			return u;
		}

		protected override Expression VisitBinary(BinaryExpression b)
		{
			sb.Append("(");
			Visit(b.Left);
			switch (b.NodeType)
			{
				case ExpressionType.And:
				case ExpressionType.AndAlso:
					sb.Append(" AND ");
					break;
				case ExpressionType.Or:
				case ExpressionType.OrElse:
					sb.Append(" OR ");
					break;
				case ExpressionType.Equal:
					sb.Append(" = ");
					break;
				case ExpressionType.NotEqual:
					sb.Append(" <> ");
					break;
				case ExpressionType.LessThan:
					sb.Append(" < ");
					break;
				case ExpressionType.LessThanOrEqual:
					sb.Append(" <= ");
					break;
				case ExpressionType.GreaterThan:
					sb.Append(" > ");
					break;
				case ExpressionType.GreaterThanOrEqual:
					sb.Append(" >= ");
					break;
				default:
					throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
			}
			Visit(b.Right);
			sb.Append(")");

			return b;

		}

		protected override Expression VisitConstant(ConstantExpression c)
		{
			if (c.Value == null)
			{
				if (sb[sb.Length - 2] == '=')
				{
					sb.Remove(sb.Length - 2, 2);
					sb.Append("is NULL ");
				}
				else sb.Append("NULL ");
			}
			else
			{
				switch (Type.GetTypeCode(c.Value.GetType()))
				{
					case TypeCode.Boolean:
						sb.Append(((bool)c.Value) ? "'true'" : "'false'");
						break;
					case TypeCode.String:
						sb.Append($"'{c.Value}'");
						break;
					case TypeCode.Object:
						throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));
					default:
						sb.Append(c.Value);
						break;
				}
			}
			return c;
		}

		protected override MemberBinding VisitMemberBinding(MemberBinding node)
		{
			throw new NotSupportedException();
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
		{
			throw new NotSupportedException();
		}

		protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
		{
			throw new NotSupportedException();
		}

		protected override ElementInit VisitElementInit(ElementInit node)
		{
			throw new NotSupportedException();
		}

		protected override LabelTarget VisitLabelTarget(LabelTarget node)
		{
			throw new NotSupportedException();
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
		{
			throw new NotSupportedException();
		}

		protected override CatchBlock VisitCatchBlock(CatchBlock node)
		{
			throw new NotSupportedException();
		}

		protected override SwitchCase VisitSwitchCase(SwitchCase node)
		{
			throw new NotSupportedException();
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			if (typeof(EQEventInfo).IsAssignableFrom(node.Expression.Type))
			{
				sb.Append($" @{node.Member.Name} ");
				args.Add(Reflex.GetMember(inputObject, node.Member.Name));
			}
			else if(typeof(WorkItem).IsAssignableFrom(node.Expression.Type))
			{
                switch(node.Member.Name.ToLower())
                {
                    case "itemid":
                        sb.Append($" ItemID ");  // NOTE: Maps directly to the ItemID column
                        break;
                    case "projectid":
                        sb.Append($" ProjectID ");  // NOTE: This is supported by Computed Column 'ProjectID' added directly in ItemData
                        break;
                    case "orderid":
                        sb.Append($" OrderID ");  // NOTE: This is supported by Computed Column 'OrderID' added directly in ItemData
                        break;
                    default:
                        sb.Append($" JSON_VALUE(ItemState, '$.{node.Member.Name}') ");
                        break;
                }
			}
			return node;
		}
	}
}
