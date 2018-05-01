/**
 * Provides a wrapper to cleanly resolve promises from the command library.
 */

 const Discord = require('discord.js');

 module.exports = new class CommandResponse {
     constructor({channel, content, options = {}}) {
         if (!(channel instanceof Discord.Channel)) {
             throw new TypeError(`channel is not an instanceof Discord.Channel`);
         }

         if (typeof content !== 'string') {
             throw new TypeError(`content is not a string`);
         }

         if (typeof options !== 'object') {
             throw new TypeError(`options is not an object`);
         }

         Object.defineProperties(this, {
             channel: {
                 enumerable: true,
                 get: () => channel
             },
             content: {
                 enumerable: true,
                 get: () => content
             },
             options:  {
                enumerable: true,
                get: () => options
            },
         });
     }
 }
 