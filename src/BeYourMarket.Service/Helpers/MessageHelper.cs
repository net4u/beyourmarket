﻿using BeYourMarket.Core;
using BeYourMarket.Model.Enum;
using BeYourMarket.Model.Models;
using BeYourMarket.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Repository.Pattern.UnitOfWork;

namespace BeYourMarket.Service
{
    public static class MessageHelper
    {
        public static MessageReadStateService MessageReadStateService
        {
            get
            {
                return ContainerManager.GetConfiguredContainer().Resolve<BeYourMarket.Service.MessageReadStateService>();
            }
        }

        public static MessageService MessageService
        {
            get
            {
                return ContainerManager.GetConfiguredContainer().Resolve<BeYourMarket.Service.MessageService>();
            }
        }

        public static MessageThreadService MessageThreadService
        {
            get
            {
                return ContainerManager.GetConfiguredContainer().Resolve<BeYourMarket.Service.MessageThreadService>();
            }
        }

        public static MessageParticipantService MessageParticipantService
        {
            get
            {
                return ContainerManager.GetConfiguredContainer().Resolve<BeYourMarket.Service.MessageParticipantService>();
            }
        }

        public static IUnitOfWorkAsync UnitOfWorkAsync
        {
            get
            {
                return ContainerManager.GetConfiguredContainer().Resolve<IUnitOfWorkAsync>();
            }
        }


        /// <summary>
        /// Get UnRead Messages
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<Message> GetUnReadMessages(string userId)
        {
            var messageReadStates = MessageReadStateService
                .Query(x => x.UserID == userId && !x.ReadDate.HasValue)
                .Include(x => x.Message)
                .Include(x => x.Message.AspNetUser)
                .Select();

            var messages = messageReadStates
                .OrderByDescending(x => x.Created)
                .Select(x => x.Message)
                .ToList();

            return messages;
        }

        /// <summary>
        /// Send Message
        /// </summary>
        /// <param name="userFrom"></param>
        /// <param name="userTo"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static async Task SendMessage(string userFrom, string userTo, string subject, string body)
        {
            var unitOfWork = ContainerManager.GetConfiguredContainer().Resolve<IUnitOfWorkAsync>();

            var messageThreadQuery = await MessageThreadService
                .Query(x => 
                    x.Subject.Equals(subject, StringComparison.InvariantCultureIgnoreCase) &&
                    x.MessageParticipants.Any(y => y.UserID == userFrom) &&
                    x.MessageParticipants.Any(y => y.UserID == userTo))
                .SelectAsync();

            var messageThread = messageThreadQuery.FirstOrDefault();

            // Create message thread if there is not one yet.
            if (messageThread == null)
            {
                messageThread = new MessageThread()
                {
                    Subject = subject,
                    Created = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
                };

                MessageThreadService.Insert(messageThread);

                await UnitOfWorkAsync.SaveChangesAsync();

                // Add message participants
                MessageParticipantService.Insert(new MessageParticipant()
                {
                    UserID = userFrom,
                    MessageThreadID = messageThread.ID,
                    ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
                });

                MessageParticipantService.Insert(new MessageParticipant()
                {
                    UserID = userTo,
                    MessageThreadID = messageThread.ID,
                    ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
                });
            }

            // Insert mail message to db
            var message = new Message()
            {
                UserFrom = userFrom,
                Body = body,
                MessageThreadID = messageThread.ID,
                Created = DateTime.Now,
                LastUpdated = DateTime.Now,
                ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
            };

            MessageService.Insert(message);

            await UnitOfWorkAsync.SaveChangesAsync();

            // Add read state of messages
            MessageReadStateService.Insert(new MessageReadState()
            {
                MessageID = message.ID,
                UserID = userFrom,
                ReadDate = DateTime.Now,
                ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
            });

            MessageReadStateService.Insert(new MessageReadState()
            {
                MessageID = message.ID,
                UserID = userTo,
                ReadDate = null,
                ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
            });

            await UnitOfWorkAsync.SaveChangesAsync();
        }
    }
}
