/**
* License and Terms of Use
*
* Copyright (c) 2011 SignpostMarv
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/
(function(window, undefined){
	window['mapapi'] = window['mapapi'] || {};
	var
		document     = window['document'],
		mapapi       = window['mapapi']
	;

	var tileSource = function(options){
		var
			obj        = this,
			objopts    = (obj['options'] = {}),
			options    = options || {},
			copyright  = options['copyright'],
			label      = options['label'],
			minZoom    = options['minZoom'] || 0,
			maxZoom    = options['maxZoom'] || 0,
			bgColor    = options['backgroundColor'] || '#000000',
			width      = Math.max(1, options['width'] || 256),
			height     = Math.max(1, options['height'] || width),
			mimeType   = options['mimeType'] || 'image/jpeg',
			opacity    = Math.max(0, Math.min(1, options['opacity'] || 1))
		;

		if(!copyright){
			throw 'tile source copyright not specified';
		}else if(!label){
			throw 'tile source label not specified';
		}

		obj['size']                = new mapapi['size'](width, height)

		objopts['copyright']       = copyright;
		objopts['label']           = label;
		objopts['minZoom']         = Math.max(minZoom, 0);
		objopts['maxZoom']         = Math.max(objopts['minZoom'] + 1, maxZoom);
		objopts['backgroundColor'] = bgColor;
		objopts['mimeType']        = mimeType;
		objopts['opacity']         = opacity;
	}

	tileSource.prototype.getTileURL = function(pos, zoom){
		return 'data:text/plain,';
	}

	mapapi['tileSource'] = tileSource;
	mapapi['tileSource'].prototype['getTileURL'] = tileSource.prototype.getTileURL;
})(window);