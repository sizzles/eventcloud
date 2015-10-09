﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using Abp.UI;
using EventCloud.Domain.Events;

namespace EventCloud.Events
{
    [Table("AppEvents")]
    public class Event : FullAuditedEntity<Guid>
    {
        public const int MaxTitleLength = 128;
        public const int MaxDescriptionLength = 128;

        [Required]
        [StringLength(MaxTitleLength)]
        public virtual string Title { get; protected set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; protected set; }

        public virtual DateTime Date { get; protected set; }

        [Range(0, 60)]
        public virtual int MinAgeToRegister { get; protected set; }

        public virtual bool IsCancelled { get; protected set; }

        /// <summary>
        /// We don't make constructor public and forcing to create events using <see cref="Create"/> method.
        /// But constructor can not be private since it's used by EntityFramework.
        /// Thats why we did it protected.
        /// </summary>
        protected Event()
        {

        }

        public static Event Create(string title, DateTime date, string description = null, int minAgeToRegister = 0)
        {
            var @event = new Event
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                MinAgeToRegister = minAgeToRegister,
            };

            @event.SetDate(date);

            return @event;
        }

        public bool IsInPast()
        {
            return Date < Clock.Now;
        }

        public bool IsAllowedCancellationTimeEnded()
        {
            return Date.Subtract(Clock.Now).TotalHours <= 2.0; //2 hours can be defined as Event property and determined per event
        }

        public void SetDate(DateTime date)
        {
            AssertNotCancelled();

            if (date < Clock.Now)
            {
                throw new UserFriendlyException("Can not set an event's date in the past!");
            }

            if (date <= Clock.Now.AddHours(3)) //3 can be configurable per tenant
            {
                throw new UserFriendlyException("Should set an event's date 3 hours before at least!");
            }

            Date = date;
        }

        public void Cancel()
        {
            AssertNotInPast();
            IsCancelled = true;

            DomainEvents.EventBus.Trigger(new EventCancelledEvent(this));
        }

        private void AssertNotInPast()
        {
            if (IsInPast())
            {
                throw new UserFriendlyException("This event was in the past");
            }
        }

        private void AssertNotCancelled()
        {
            if (IsCancelled)
            {
                throw new UserFriendlyException("This event is canceled!");
            }
        }
    }
}