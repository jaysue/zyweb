function touchSwiper() {
    "use strict";
    var that = {};
    that.events = {};
    //hold the elems id's
    var configs = {
        coverFlowElement: null,
        boxGames: 'h-listing'
    };
    //main function that gets init
    that.init = function () {
        // for andriod devices remove the swipe effect because the UX it's bad
        var device = that.getDeviceType();
        //apply the modern slider only for ios devices with ios bigger then 5
        if ( device.ios === true && device.osV >= 5 ) {
            that.touchSwiperModern();
        }
        //applies for android devices because they have a poorly support for overflow
        else {
            //ini slider for the games box
            var boxGames = document.getElementById(configs.boxGames);
            //init for your apps box games
            if ( boxGames && boxGames != undefined ) {
                touchSwipe({
                    container: boxGames
                });
            }
        }
    };
    //modern slider based on overflow scroll for newer ios versions
    that.touchSwiperModern = function() {
        configs.coverFlowElement = document.getElementsByClassName(configs.boxGames)
        var x, len;
        for ( x = 0, len = configs.coverFlowElement.length; x < len; x++ ) {
            configs.coverFlowElement[x].style.webkitOverflowScrolling = 'touch';
            configs.coverFlowElement[x].style.overflowX = 'scroll';
        }
    };
    //returns ios or android
    //mainly needed for ios to detect new versions to apply modernSlider
    that.getDeviceType = function() {
        var ua = navigator.userAgent,
            ios = ( ua.match(/(iPad|iPhone|iPod)/g) ? true : false ),
            android = ( ua.match(/(Android)/g) ? true : false ), v = 0;
        if ( ios === true ) {
            v = (navigator.appVersion).match(/OS (\d+)_(\d+)_?(\d+)?/),
            v = [parseInt(v[1], 10), parseInt(v[2], 10), parseInt(v[3] || 0, 10)];
        }
        var deviceType = {
            ios: ios,
            osV : v[0] || 0,
            android: android
        };
        return deviceType;
    };
    //private method not to be accesible from page
    var touchSwipe = function(config) {
        var mainContainer = config.container,
            options = {
            holder: 'h-listing-holder',
            item: 'listing',
            mode: 'shift',
            shift: '80%',
            delta: 40,
            center: false,
            animation: 'auto',
            touch: true,
            duration: 500,
            onChange: null,
            onStart: null,
            activeClass: 'is-active',
            minChildren: 3,
            currentIndex : null,
            currentOffset : null,
            graphRealIndex : 0,
            graphItems : [],
            previousIndex : null,
            previousOffset : null,
            touchOffset : 0,
            hasChanges : false,
            itemsGap : 15
        },
        events =  [
            {
                start: 'mousedown',
                end: 'mouseup',
                move: 'mousemove',
                leave: 'mouseout'
            },
            {
                start: 'touchstart',
                end: 'touchend',
                move: 'touchmove',
                leave: 'touchend'
            }
        ];
        //global that are used by the slider
        var eventIndex = "ontouchend" in document ? 1 : 0,
            isTouching = false, startX = 0,cssTransitionsSupported = false,
            has3D = ('WebKitCSSMatrix' in window && 'm11' in new WebKitCSSMatrix()),
            width = 0, startX = 0;

        var touchSwipeDefault = function() {
            //main div that holds the listing
            var holder = document.getElementsByClassName(options.holder);
            if (!holder) return;

            if (!mainContainer) return;
            //box games childrens li's
            var items = mainContainer.children;
            if (!items) return;
            //if the slider box has more then 3 children then we can
            if (items.length <= options.minChildren) return;

            //check if the animation is set in the config
            if (options.animation) {
                //check if css transitions are supported
                var bodyStyle = document.body.style,
                    transitionEndEvent = (bodyStyle.WebkitTransition !== undefined) ? "webkitTransitionEnd" : "transitionend";
                cssTransitionsSupported = bodyStyle.WebkitTransition !== undefined || bodyStyle.MozTransition !== undefined || bodyStyle.transition !== undefined;
                //check if transition are sported and if animation is set to auto
                if (cssTransitionsSupported && options.animation == 'auto') {
                    //set default style on the boxes because they will perform the animations
                    setDefaultStyle(mainContainer);
                }
            }
            //starting index and offset
            setCurrentIndex(0);
            setCurrentOffset(0);
            //check if touch it's enabled
            if( options.touch ) {
                //add thouch events listener to box
                bindEvents(mainContainer);
            }
            //on resize update the slider elements
            window.addEventListener("onorientationchange" in window ? "orientationchange" : "resize", this, resizeEvent);
            initPosition(true);
        };
        //default style that applies on the box items
        var setDefaultStyle = function(el) {
            setStyle(el, {
                '-webkit-transition-property': '-webkit-transform',
                '-webkit-transition-timing-function': 'ease',
                '-moz-transition-property': '-moz-transform',
                '-moz-transition-timing-function': 'ease',
                'transition-property': 'transform',
                'transition-timing-function': 'ease'
            });
        };
        //returns the width of the holder
        var getWidth = function(el) {
            var width = el.offsetWidth;
            return width;
        };
        //on resize set timeout and call the position
        var resizeEvent = function() {
            setTimeout(initPosition, 200);
        };
        //get the touch event type
        var touchEvent = function(e) {
            var touch = (eventIndex) ? e.touches[0] : e;
            switch(e.type)
            {
                case events[eventIndex]['move']:
                    touchMove(e, touch);
                    break;
                case events[eventIndex]['start']:
                    touchStart(e, touch);
                    break;
                case events[eventIndex]['end']:
                case events[eventIndex]['leave']:
                    touchEnd(e);
                    break;
            }
        };
        var touchStart = function(e, touch) {
            if (!isTouching) {
                options.hasChanges = false;
                isTouching = true;
                options.touchOffset = 0;
                startX = (eventIndex) ? touch.pageX : e.clientX;
            }
        };
        //on swipe get the offset and the swiped value and send it to the moveTo fn
        var touchMove = function(e, touch) {
            if (isTouching) {
                e.preventDefault();
                options.touchOffset = startX - touch.pageX;
                if (options.touchOffset != 0) {
                    moveTo(options.currentOffset - options.touchOffset, true);
                }
            }
        };
        var touchEnd = function(e, touch) {
            if (isTouching) {
                isTouching = false;
                startX = 0;
                if (options.touchOffset != 0) {
                    if (options.touchOffset > options.delta || options.touchOffset < (0 - options.delta)) {
                        moveShift();
                    } else {
                        moveTo(options.currentOffset);
                    }
                }
            }
            options.touchOffset = 0;
        };
        //init the position of the slider
        //call moveshift on swipe
        var initPosition = function(init) {
            var holder = document.getElementsByClassName(options.holder)[0];
            width = holder.offsetWidth;
            options.touchOffset = 0;
            var initi = typeof(init) == 'undefined' ? false: init;
            if (initi && typeof(options.onStart) === 'function') {
                options.onStart();
            }
            moveShift();
        };
        var moveShift = function() {
            var boxItems, itemsWidth = 0, shift = options.shift + '', possibleOffset = 0;
            //get the width of the children and add it to the itemswidth
            boxItems = mainContainer.children;
            for ( var x = 0; x < boxItems.length; x++) {
                itemsWidth += boxItems[x].offsetWidth + options.itemsGap;
            }
            doMoveTo(shift, possibleOffset, itemsWidth);
        };
        var doMoveTo = function(shift, possibleOffset, itemsWidth) {
            //check if the mode it's shift (swipe)
            if(shift.indexOf('%') != -1) {
                //width of the box * 80% (max scroll) / 80
                shift = parseInt( width * options.shift.substr(0, options.shift.length-1) / 80);
            } else {
                //if it's not shift then just get the default val 80%
                shift = parseInt(options.shift);
            }
            //check if starts from center and if the width of children is smaller then the holder width
            //if so then call the moveTo function with the current offset
            if (options.center && itemsWidth < width) {
                possibleOffset = (width / 2) - (itemsWidth / 2);
                setCurrentOffset(possibleOffset);
                moveTo(options.currentOffset);
            } else {
                //check if the offset is bigger then 0 then move the slider
                if (options.touchOffset > 0) {
                    possibleOffset = options.currentOffset - shift;
                    if(itemsWidth + possibleOffset > width) {
                        setCurrentOffset(possibleOffset);
                        moveTo(options.currentOffset);
                    } else {
                        setCurrentOffset(0 - (itemsWidth - width));
                        moveTo(options.currentOffset);
                    }
                } else {
                    possibleOffset = options.currentOffset + shift;
                    if(possibleOffset < 0) {
                        setCurrentOffset(possibleOffset);
                        moveTo(options.currentOffset);
                    } else {
                        setCurrentOffset(0);
                        moveTo(options.currentOffset);
                    }
                }
            }
        };
        //move to the give coordinates and apply a delay for animation effect
        var moveTo = function(coord, now) {
            var delay = typeof(now) == 'undefined' ? options.duration : 0;

            if (typeof(now) == 'undefined' && typeof(options.onChange) === 'function' && options.hasChanges) {
                options.onChange(options.previousIndex, options.currentIndex);
            }
            moveToAction(delay, mainContainer, coord, now);
        };
        var moveToAction = function(delay, el, coord, now) {
            //if there is no animation enabled start set style to the box and end animation
            if (!options.animation) {
                if (delay) {
                    startAnimation(el);
                    setStyle(el, {
                        'margin-left': coord + 'px'
                    });
                    endAnimation();
                }
                //if the animation is set start the animation on the box
                //set a delay so the animation is visible and apply the moving coordonates
            } else {
                startAnimation(el);
                var delayTransition = typeof(now) == 'undefined' ? options.duration/1000+'s' : '0';
                if (cssTransitionsSupported) {
                    setStyle(el, {
                        '-webkit-transition-duration': delayTransition,
                        '-moz-transition-duration': delayTransition,
                        'transition-duration': delayTransition
                    });
                    //if webkit translate is supported move the slider with translate3d css option
                    if(has3D) {
                        setStyle(el, {
                            '-webkit-transform': 'translate3d('+coord+'px,0,0)',
                            'transform': 'translate3d('+coord+'px,0,0)'
                        });
                        //if the translate3d is not supported then just move the slider with translate
                    }else {
                        setStyle(el, {
                            '-webkit-transform': 'translate('+coord+'px,0)',
                            '-moz-transform': 'translate('+coord+'px,0)',
                            'transform': 'translate('+coord+'px,0)'
                        });
                    }
                    //if the css3 is not supported do the animation old style with margin-left and apply a delay animation
                } else {
                    el.setInterval(function(){
                        setStyle(el, {
                            'margin-left': coord+'px'
                        });
                        if ( el.style.marginLeft === coord ) {
                            endAnimation(el);
                        }
                    }, delay);
                }
            }
        };
        //set current index of the swiped items
        var setCurrentIndex = function(index) {
            console.log('Index ' + index)
            var items = document.getElementsByClassName(options.item);
            if (index != options.currentIndex) {
                options.hasChanges = true;
                options.previousIndex = options.currentIndex;
                options.currentIndex = index;
                if (options.mode == 'auto' && options.graphItems.length) {
                    options.graphRealIndex = options.graphItems[options.currentIndex][1][0][0];
                } else {
                    items.className = 'listing';
                    items[options.currentIndex].className = 'listing ' + options.activeClass;
                }
            } else {
                options.hasChanges = true;
            }
        };
        //set the swiped offset
        var setCurrentOffset = function(offset) {
            console.log(offset)
            offset = parseInt(offset, 10);
            if (offset != options.currentOffset) {
                options.previousOffset = options.currentOffset;
                options.currentOffset = offset;
            }
        };
        //give an object as style and here is applied to the element
        var setStyle = function(el, properties) {
            for ( var prop in properties ) {
                el.style[prop] = properties[prop];
            }
        };
        //apply the moving class on start animation
        var startAnimation = function(box) {
            box.className = 'h-listing moving default';
        };
        //remove the moving class
        var endAnimation = function(box) {
            box.className = 'h-listing default';
        };
        //create event listener on the given element
        var bindEvents = function(el,other, event, fn) {
            el.addEventListener(events[eventIndex]['start'], touchEvent);
            el.addEventListener(events[eventIndex]['end'], touchEvent);
            el.addEventListener(events[eventIndex]['leave'], touchEvent);
            el.addEventListener(events[eventIndex]['move'], touchEvent);
        };
        //remove event listener on the given element
        var unbindEvent = function(el, event){
            for (var x = 0; x < el.length; x++) {
                el[x].removeEventListener(event, function(){}, false);
            }
        };
        return touchSwipeDefault();

    };

    return that;
}

window.onload = function() {
    touchSwiper().init();
}   

