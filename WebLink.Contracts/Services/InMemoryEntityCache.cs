using Microsoft.CodeAnalysis.CSharp.Syntax;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
	public class InMemoryEntityCache<TEntity>  : IDisposable
		where TEntity : class, IEntity
	{
		private readonly IEventQueue events;
		private readonly string token;
		private readonly ConcurrentDictionary<int, IEntity> index;
        private Func<int, TEntity> findCallback;

		public InMemoryEntityCache(IEventQueue events)
		{
			this.events = events;
			index = new ConcurrentDictionary<int, IEntity>();
			token = events.Subscribe<EntityEvent>(HandleEvent);
		}

		public void Dispose()
		{
			events.Unsubscribe<EntityEvent>(token);
		}

		public void Initialize(IEnumerable<TEntity> values, Func <int, TEntity> findCallback)
		{
            this.findCallback = findCallback;    
			index.Clear();
			foreach (var element in values)
				index[element.ID] = element;
		}

		public TEntity GetByID(int id)
		{
			if (index.TryGetValue(id, out var entity))
				return (TEntity)entity;

            entity = findCallback(id);  
            if (entity != null)
                index[id] = entity;

            return (TEntity)entity;
		}

		private void HandleEvent(EntityEvent e)
		{
			if(e.EntityType == typeof(TEntity).FullName)
			{
				switch(e.Operation)
				{
					case DBOperation.Insert:
						index.TryAdd(e.Entity.ID, e.Entity);
						break;
					case DBOperation.Update:
						index[e.Entity.ID] = e.Entity;
						break;
					case DBOperation.Delete:
						index.TryRemove(e.Entity.ID, out _);
						break;
				}
			}
		}
	}
}
