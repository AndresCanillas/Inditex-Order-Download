using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Service.Contracts.WF
{
	public class ItemStateQueryTranslator : ExpressionVisitor
	{
		private string alias;
		private ItemStateVisitor defaultVisitor = new ItemStateVisitor();
		private ItemStateVisitor inVisitor = new INConstantVisitor();
		private ItemStateVisitor visitor;

		private StringBuilder sb;

		public ItemStateQueryTranslator()
		{
			visitor = defaultVisitor;
		}

		public string Translate(Expression expression, string alias = null)
		{
			sb = new StringBuilder(1000);

			this.alias = alias;
			if (this.alias != null && !this.alias.EndsWith("."))
				this.alias += '.';

			defaultVisitor.Alias = this.alias;
			inVisitor.Alias = this.alias;

			Visit(expression);
			return sb.ToString();
		}

		protected override Expression VisitMethodCall(MethodCallExpression m)
		{
			if(m.Method.Name == "IN")
			{
				Visit(m.Arguments[0]);
				visitor = inVisitor;
				visitor.Reset();
				Visit(m.Arguments[1]);
				sb.Append($"IN ({visitor})");
				visitor = defaultVisitor;
				return m;
			}
            else
            {
                 throw new NotImplementedException($"Method {m.Method.Name} is not supported as part of an ItemState expression.");
            }
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
			visitor.VisitConstant(sb, c);
			return c;
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			visitor.VisitMember(sb, node);
			return node;
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
	}

	class ItemStateVisitor
	{
		public virtual void Reset() { }

		public string Alias { get;set; }

		public virtual void VisitConstant(StringBuilder sb, ConstantExpression c)
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
		}

		public virtual void VisitMember(StringBuilder sb, MemberExpression node)
		{
            if(typeof(WorkItem).IsAssignableFrom(node.Expression.Type))
            {
                switch(node.Member.Name.ToLower())
                {
                    case "itemid":
                        sb.Append($" {Alias}ItemID ");  // NOTE: Maps directly to the ItemID column
                        break;
                    case "projectid":
                        sb.Append($" {Alias}ProjectID ");  // NOTE: This is supported by Computed Column 'ProjectID' added directly in ItemData
                        break;
                    case "orderid":
                        sb.Append($" {Alias}OrderID ");  // NOTE: This is supported by Computed Column 'OrderID' added directly in ItemData
                        break;
                    default:
                        sb.Append($" JSON_VALUE({Alias}ItemState, '$.{node.Member.Name}') ");
                        break;
                }
            }
            else throw new InvalidOperationException("Expression must operate on a WorkItem");
		}
	}

	class INConstantVisitor : ItemStateVisitor
	{
		private List<string> elements = new List<string>();

		public override void Reset()
		{
			elements.Clear();
		}

		public override void VisitConstant(StringBuilder sb, ConstantExpression c)
		{
			if (c.Value != null)
			{
				switch (Type.GetTypeCode(c.Value.GetType()))
				{
					case TypeCode.Boolean:
						elements.Add(((bool)c.Value) ? "'true'" : "'false'");
						break;
					case TypeCode.String:
						elements.Add($"'{c.Value}'");
						break;
					case TypeCode.Object:
						throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));
					default:
						elements.Add(c.Value.ToString());
						break;
				}
			}
		}

		private void AddMemberValue(object value, string memberName)
		{
			var member = value.GetType().GetMember(memberName, BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
			if (member == null)
				return;

			if (member is PropertyInfo pinfo)
			{
				var v = pinfo.GetValue(value);
				if (v != null)
					elements.Add(ItemDataModel.FormatJsonValue(v));
			}
			else if (member is FieldInfo finfo)
			{
				var v = finfo.GetValue(value);
				if (v != null)
					elements.Add(ItemDataModel.FormatJsonValue(v));
			}
		}

		public override void VisitMember(StringBuilder sb, MemberExpression node)
		{
			if (node.Expression is ConstantExpression constantExpression)
			{
				var value = Reflex.GetMember(constantExpression.Value, node.Member.Name);
				if (value is IEnumerable enumerable)
				{
					foreach (var v in enumerable)
					{
						if (v != null)
							elements.Add(ItemDataModel.FormatJsonValue(v));
					}
				}
				else
					AddMemberValue(constantExpression.Value, node.Member.Name);
			}
			else throw new InvalidOperationException("IN must operate on constants");
		}

		public override string ToString()
		{
			return elements.Merge(",");
		}
	}

	public static class ItemStateQueryExtensions
	{
		public static bool IN(this string field, IEnumerable<string> values)
		{
			if(values == null || field == null)
				return false;

			return values.Contains(field);
		}

		public static bool IN(this string field, params string[] values) => IN(field, (IEnumerable<string>)values);

		public static bool IN(this int field, IEnumerable<int> values)
		{
			if (values == null)
				return false;

			return values.Contains(field);
		}

		public static bool IN(this int field, params int[] values) => IN(field, (IEnumerable<int>)values);
	}
}
