//
//   CSharpBender - https://github.com/CSharpBender/NHUnit
//
// License: MIT
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//
using NHibernate;
using NHibernate.Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NHUnit
{
    internal static class NhibernateHelper
    {
        public static List<T> Unproxy<T>(List<T> entityList, ISession session)
        {
            var resolvedEntities = new HashSet<object>(); //avoid infinite loop
            for (int i = 0; i < entityList.Count; i++)
            {
                entityList[i] = Unproxy<T>(entityList[i], session, resolvedEntities);
            }

            return entityList;
        }

        public static T Unproxy<T>(T entity, ISession session)
        {
            return Unproxy<T>(entity, session, new HashSet<object>());
        }

        private static T Unproxy<T>(T entity, ISession session, HashSet<object> resolvedEntities)
        {
            if (entity == null)
                return default(T);

            T resolvedEntity;
            if (entity is INHibernateProxy)
            {
                if (!NHibernateUtil.IsInitialized(entity))
                {
                    return default(T);
                }
                resolvedEntity = (T)session.GetSessionImplementation().PersistenceContext.Unproxy(entity);
            }
            else
            {
                resolvedEntity = entity;
            }

            if (resolvedEntities.Contains(resolvedEntity))
                return resolvedEntity;

            resolvedEntities.Add(resolvedEntity);

            Type entityType = resolvedEntity.GetType();
            var entityMetadata = session.SessionFactory.GetClassMetadata(entityType);
            if (entityMetadata == null)
            {
                if (IsAnonymousType(entityType))
                {
                    foreach (PropertyInfo prop in entityType.GetProperties())
                    {
                        var propValue = prop.GetValue(entity, null);
                        Unproxy(propValue, session, resolvedEntities);
                    }
                    return entity;
                }
                else
                {
                    return default(T);
                }
            }

            PropertyInfo[] propertyInfos = entityType.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                var propertyName = propertyInfo.Name;
                var entityPropertyType = entityMetadata.GetPropertyType(propertyName);
                var propertyValue = propertyInfo.GetValue(entity, null);
                if (entityPropertyType.IsCollectionType)
                {
                    var propertyListPublicType = propertyInfo.PropertyType.GetGenericArguments()[0];
                    var propertyListType = typeof(List<>).MakeGenericType(propertyListPublicType);
                    IList propertyList = (IList)Activator.CreateInstance(propertyListType);
                    propertyInfo.SetValue(resolvedEntity, propertyList, null);

                    if (NHibernateUtil.IsInitialized(propertyValue))
                    {
                        foreach (var propValue in (IEnumerable)propertyValue)
                        {
                            propertyList.Add(Unproxy(propValue, session, resolvedEntities));
                        }
                    }
                }
                else if (entityPropertyType.IsEntityType)
                {
                    propertyInfo.SetValue(resolvedEntity, Unproxy(propertyValue, session, resolvedEntities), null);
                }
            }

            return resolvedEntity;
        }

        public static bool IsAnonymousType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.IsGenericType
                   && type.Name.Contains("AnonymousType");
        }

        public static void VisitNodes(object obj, ISession session, EntityNodeInfo includeChildNodes)
        {
            if (obj is null || includeChildNodes?.Children == null)
            {
                return;
            }
            if (obj is IEnumerable)
            {
                foreach (var o in (IEnumerable)obj)
                {
                    VisitNodes(o, session, includeChildNodes);
                }
                return;
            }

            var objType = obj.GetType();

            bool isInitialized = false;
            foreach (var childExpr in includeChildNodes.Children)
            {
                var childProp = objType.GetProperty(childExpr.Name);
                var childValue = childProp.GetValue(obj);
                VisitNodes(childValue, session, childExpr);
                isInitialized = true;
            }
            if (!isInitialized)
            {
                NHibernateUtil.Initialize(obj);
            }
        }

        public static EntityNodeInfo GetExpressionTreeInfo<T>(Expression<Func<T, object>>[] includeChildNodes, EntityNodeInfo rootNode, bool includeParent = false)
        {
            foreach (var includeChildExpression in includeChildNodes)
            {
                var expression = includeChildExpression.Body;
                var propertiesInfo = new Stack<(string PropertyPath, Type PropertyType)>();
                while (expression != null)
                {
                    var expr = (expression as MemberExpression) ?? (expression as UnaryExpression)?.Operand as MemberExpression;
                    if (expr == null)
                    {
                        if (expression is MethodCallExpression)
                        {
                            var meth = (MethodCallExpression)expression;
                            expression = meth.Object ?? meth.Arguments?.FirstOrDefault();
                            continue;
                        }
                        break;
                    }

                    var memberExpression = (expression as MemberExpression) ?? (expression as UnaryExpression)?.Operand as MemberExpression;
                    var propertyType = (memberExpression?.Member as PropertyInfo)?.PropertyType ?? typeof(object);
                    propertiesInfo.Push((expr.Member.Name, propertyType)); //returns the last property name
                    expression = expr.Expression;
                }

                int level = 1;
                var exprNode = rootNode;
                StringBuilder sb = includeParent ? new StringBuilder() : null;
                foreach (var propertyInfo in propertiesInfo)
                {
                    string propertyPath = propertyInfo.PropertyPath;
                    var existingNode = exprNode.Children.FirstOrDefault(c => c.Name == propertyPath);
                    if (existingNode == null)
                    {
                        var isList = propertyInfo.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType);
                        var newNode = new EntityNodeInfo() { Name = propertyPath, IsList = isList, Level = level };
                        if (includeParent)
                        {
                            if (sb.Length > 0)
                            {
                                sb.Append('.');
                            }

                            sb.Append(propertyInfo.PropertyPath);
                            newNode.PathName = sb.ToString();
                        }
                        exprNode.Children.Add(newNode);
                        exprNode = newNode;
                    }
                    else
                    {
                        exprNode = existingNode;
                    }
                    level++;
                }
            }

            return rootNode;
        }
    }
}
