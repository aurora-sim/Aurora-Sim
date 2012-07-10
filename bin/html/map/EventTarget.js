/**
* @license Copyright (c) 2010 Nicholas C. Zakas. All rights reserved.
* MIT License
*/
(function(window, undefined){
	function EventTarget(){
		this['_listeners'] = {};
	}

	EventTarget['prototype'] = {

		'constructor': EventTarget,

		'addListener': function(type, listener){
			if (this['_listeners'][type] == undefined){
				this['_listeners'][type] = [];
			}
			this['_listeners'][type].push(listener);
			return listener;
		},

		'fire': function(event, args){
			var
				event = typeof event == 'string' ? {'type':event} : {'type':event['type']},
				type = event['type'],
				args = args || {}
			;
			for(var i in args){
				event[i] = args[i];
			}
			event['target'] = this;

			if (!type){
				throw new 'Event object missing \'type\' property.';
			}

			if (this['_listeners'][type] instanceof Array){
				var
					listeners = this['_listeners'][type]
				;
				for (var i=0; i < listeners.length; i++){
					listeners[i].call(this, event);
				}
			}
		},

		'removeListener': function(type, listener){
			if (this['_listeners'][type] instanceof Array){
				var listeners = this['_listeners'][type];
				for (var i=0, len=listeners.length; i < len; i++){
					if (listeners[i] === listener){
						listeners.splice(i, 1);
						break;
					}
				}
			}
		}
	};

	window['EventTarget'] = EventTarget;
})(window);
