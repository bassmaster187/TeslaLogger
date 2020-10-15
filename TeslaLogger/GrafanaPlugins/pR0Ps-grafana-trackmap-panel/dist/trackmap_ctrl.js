System.register(["./leaflet/leaflet.js", "moment", "app/core/app_events", "app/plugins/sdk", "./leaflet/leaflet.css!", "./partials/module.css!"], function (_export, _context) {
  "use strict";

  var L, moment, appEvents, MetricsPanelCtrl, TrackMapCtrl;

  function _typeof(obj) { if (typeof Symbol === "function" && typeof Symbol.iterator === "symbol") { _typeof = function _typeof(obj) { return typeof obj; }; } else { _typeof = function _typeof(obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; }; } return _typeof(obj); }

  function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

  function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

  function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

  function _possibleConstructorReturn(self, call) { if (call && (_typeof(call) === "object" || typeof call === "function")) { return call; } return _assertThisInitialized(self); }

  function _getPrototypeOf(o) { _getPrototypeOf = Object.setPrototypeOf ? Object.getPrototypeOf : function _getPrototypeOf(o) { return o.__proto__ || Object.getPrototypeOf(o); }; return _getPrototypeOf(o); }

  function _assertThisInitialized(self) { if (self === void 0) { throw new ReferenceError("this hasn't been initialised - super() hasn't been called"); } return self; }

  function _inherits(subClass, superClass) { if (typeof superClass !== "function" && superClass !== null) { throw new TypeError("Super expression must either be null or a function"); } subClass.prototype = Object.create(superClass && superClass.prototype, { constructor: { value: subClass, writable: true, configurable: true } }); if (superClass) _setPrototypeOf(subClass, superClass); }

  function _setPrototypeOf(o, p) { _setPrototypeOf = Object.setPrototypeOf || function _setPrototypeOf(o, p) { o.__proto__ = p; return o; }; return _setPrototypeOf(o, p); }

  function log(msg) {// uncomment for debugging
    //console.log(msg);
  }

  return {
    setters: [function (_leafletLeafletJs) {
      L = _leafletLeafletJs.default;
    }, function (_moment) {
      moment = _moment.default;
    }, function (_appCoreApp_events) {
      appEvents = _appCoreApp_events.default;
    }, function (_appPluginsSdk) {
      MetricsPanelCtrl = _appPluginsSdk.MetricsPanelCtrl;
    }, function (_leafletLeafletCss) {}, function (_partialsModuleCss) {}],
    execute: function () {
      _export("TrackMapCtrl", TrackMapCtrl =
      /*#__PURE__*/
      function (_MetricsPanelCtrl) {
        _inherits(TrackMapCtrl, _MetricsPanelCtrl);

        function TrackMapCtrl($scope, $injector) {
          var _this;

          _classCallCheck(this, TrackMapCtrl);

          _this = _possibleConstructorReturn(this, _getPrototypeOf(TrackMapCtrl).call(this, $scope, $injector));
          log("constructor");

          _.defaults(_this.panel, {
            maxDataPoints: 500,
            autoZoom: true,
            scrollWheelZoom: false,
            defaultLayer: 'OpenStreetMap',
            showLayerChanger: true,
            lineColor: 'red',
            pointColor: 'royalblue',
            maxDataPointDelta: 0
          }); // Save layers globally in order to use them in options


          _this.layers = {
            'OpenStreetMap': L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
              attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>',
              maxZoom: 19
            }),
            'OpenTopoMap': L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
              attribution: 'Map data: &copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)',
              maxZoom: 17
            }),
            'Satellite': L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
              attribution: 'Imagery &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community',
              // This map doesn't have labels so we force a label-only layer on top of it
              forcedOverlay: L.tileLayer('https://stamen-tiles-{s}.a.ssl.fastly.net/toner-labels/{z}/{x}/{y}.png', {
                attribution: 'Labels by <a href="http://stamen.com">Stamen Design</a>, <a href="http://creativecommons.org/licenses/by/3.0">CC BY 3.0</a> &mdash; Map data &copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>',
                subdomains: 'abcd',
                maxZoom: 20
              })
            })
          };
          _this.timeSrv = $injector.get('timeSrv');
          _this.coords = [];
          _this.leafMap = null;
          _this.layerChanger = null;
          _this.polylines = [];
          _this.superchargerMarks = [];
          _this.hoverMarker = null;
          _this.hoverTarget = null;
          _this.setSizePromise = null; // Panel events

          _this.events.on('panel-initialized', _this.onInitialized.bind(_assertThisInitialized(_this)));

          _this.events.on('view-mode-changed', _this.onViewModeChanged.bind(_assertThisInitialized(_this)));

          _this.events.on('init-edit-mode', _this.onInitEditMode.bind(_assertThisInitialized(_this)));

          _this.events.on('panel-teardown', _this.onPanelTeardown.bind(_assertThisInitialized(_this)));

          _this.events.on('panel-size-changed', _this.onPanelSizeChanged.bind(_assertThisInitialized(_this)));

          _this.events.on('data-received', _this.onDataReceived.bind(_assertThisInitialized(_this)));

          _this.events.on('data-snapshot-load', _this.onDataSnapshotLoad.bind(_assertThisInitialized(_this)));

          _this.events.on('render', _this.onRender.bind(_assertThisInitialized(_this))); // Global events


          appEvents.on('graph-hover', _this.onPanelHover.bind(_assertThisInitialized(_this)));
          appEvents.on('graph-hover-clear', _this.onPanelClear.bind(_assertThisInitialized(_this)));
          return _this;
        }

        _createClass(TrackMapCtrl, [{
          key: "onRender",
          value: function onRender() {
            var _this2 = this;

            log("onRender"); // Wait until there is at least one GridLayer with fully loaded
            // tiles before calling renderingCompleted

            if (this.leafMap) {
              this.leafMap.eachLayer(function (l) {
                if (l instanceof L.GridLayer) {
                  if (l.isLoading()) {
                    l.once('load', _this2.renderingCompleted.bind(_this2));
                  } else {
                    _this2.renderingCompleted();
                  }
                }
              });
            }
          }
        }, {
          key: "onInitialized",
          value: function onInitialized() {
            log("onInitialized");
            this.render();
          }
        }, {
          key: "onInitEditMode",
          value: function onInitEditMode() {
            log("onInitEditMode");
            this.addEditorTab('Options', 'public/plugins/pr0ps-trackmap-panel/partials/options.html', 2);
          }
        }, {
          key: "onPanelTeardown",
          value: function onPanelTeardown() {
            log("onPanelTeardown");
            this.$timeout.cancel(this.setSizePromise);
          }
        }, {
          key: "onPanelHover",
          value: function onPanelHover(evt) {
            log("onPanelHover");

            if (this.coords.length === 0) {
              return;
            } // check if we are already showing the correct hoverMarker


            var target = Math.floor(evt.pos.x);

            if (this.hoverTarget && this.hoverTarget === target) {
              return;
            } // check for initial show of the marker


            if (this.hoverTarget == null) {
              this.hoverMarker.addTo(this.leafMap);
            }

            this.hoverTarget = target; // Find the currently selected time and move the hoverMarker to it
            // Note that an exact match isn't always going to work due to rounding so
            // we clean that up later (still more efficient)

            var min = 0;
            var max = this.coords.length - 1;
            var idx = null;
            var exact = false;

            while (min <= max) {
              idx = Math.floor((max + min) / 2);

              if (this.coords[idx].timestamp === this.hoverTarget) {
                exact = true;
                break;
              } else if (this.coords[idx].timestamp < this.hoverTarget) {
                min = idx + 1;
              } else {
                max = idx - 1;
              }
            } // Correct the case where we are +1 index off


            if (!exact && idx > 0 && this.coords[idx].timestamp > this.hoverTarget) {
              idx--;
            }

            this.hoverMarker.setLatLng(this.coords[idx].position);
            this.render();
          }
        }, {
          key: "onPanelClear",
          value: function onPanelClear(evt) {
            log("onPanelClear"); // clear the highlighted circle

            this.hoverTarget = null;

            if (this.hoverMarker) {
              this.hoverMarker.removeFrom(this.leafMap);
            }
          }
        }, {
          key: "onViewModeChanged",
          value: function onViewModeChanged() {
            log("onViewModeChanged"); // KLUDGE: When the view mode is changed, panel resize events are not
            //         emitted even if the panel was resized. Work around this by telling
            //         the panel it's been resized whenever the view mode changes.

            this.onPanelSizeChanged();
          }
        }, {
          key: "onPanelSizeChanged",
          value: function onPanelSizeChanged() {
            log("onPanelSizeChanged"); // KLUDGE: This event is fired too soon - we need to delay doing the actual
            //         size invalidation until after the panel has actually been resized.

            this.$timeout.cancel(this.setSizePromise);
            var map = this.leafMap;
            this.setSizePromise = this.$timeout(function () {
              if (map) {
                log("Invalidating map size");
                map.invalidateSize(true);
              }
            }, 500);
          }
        }, {
          key: "applyScrollZoom",
          value: function applyScrollZoom() {
            var enabled = this.leafMap.scrollWheelZoom.enabled();

            if (enabled != this.panel.scrollWheelZoom) {
              if (enabled) {
                this.leafMap.scrollWheelZoom.disable();
              } else {
                this.leafMap.scrollWheelZoom.enable();
              }
            }
          }
        }, {
          key: "applyDefaultLayer",
          value: function applyDefaultLayer() {
            var _this3 = this;

            var hadMap = Boolean(this.leafMap);
            this.setupMap();

            if (hadMap) {
              // Re-add the default layer
              this.leafMap.eachLayer(function (layer) {
                layer.removeFrom(_this3.leafMap);
              });
              this.layers[this.panel.defaultLayer].addTo(this.leafMap); // Hide/show the layer switcher

              this.leafMap.removeControl(this.layerChanger);

              if (this.panel.showLayerChanger) {
                this.leafMap.addControl(this.layerChanger);
              }
            }

            this.addDataToMap();
          }
        }, {
          key: "setupMap",
          value: function setupMap() {
            var _this4 = this;

            log("setupMap"); // Create the map or get it back in a clean state if it already exists

            if (this.leafMap) {
              if (this.polylines.length > 0) {
                this.polylines.forEach(function (polyline) {
                  return polyline.removeFrom(_this4.leafMap);
                });
                this.polylines = [];
              }

              if (this.superchargerMarks.length > 0) {
                this.superchargerMarks.forEach(function (s) {
                  return s.removeFrom(_this4.leafMap);
                });
                this.superchargerMarks = [];
              }

              this.onPanelClear();
              return;
            } // Create the map


            this.leafMap = L.map('trackmap-' + this.panel.id, {
              scrollWheelZoom: this.panel.scrollWheelZoom,
              zoomSnap: 0.5,
              zoomDelta: 1
            }); // Create the layer changer

            this.layerChanger = L.control.layers(this.layers); // Add layers to the control widget

            if (this.panel.showLayerChanger) {
              this.leafMap.addControl(this.layerChanger);
            } // Add default layer to map


            this.layers[this.panel.defaultLayer].addTo(this.leafMap); // Hover marker

            this.hoverMarker = L.circleMarker(L.latLng(0, 0), {
              color: 'white',
              fillColor: this.panel.pointColor,
              fillOpacity: 1,
              weight: 2,
              radius: 7
            }); // Events

            this.leafMap.on('baselayerchange', this.mapBaseLayerChange.bind(this));
            this.leafMap.on('boxzoomend', this.mapZoomToBox.bind(this));
          }
        }, {
          key: "mapBaseLayerChange",
          value: function mapBaseLayerChange(e) {
            // If a tileLayer has a 'forcedOverlay' attribute, always enable/disable it
            // along with the layer
            if (this.leafMap.forcedOverlay) {
              this.leafMap.forcedOverlay.removeFrom(this.leafMap);
              this.leafMap.forcedOverlay = null;
            }

            var overlay = e.layer.options.forcedOverlay;

            if (overlay) {
              overlay.addTo(this.leafMap);
              overlay.setZIndex(e.layer.options.zIndex + 1);
              this.leafMap.forcedOverlay = overlay;
            }
          }
        }, {
          key: "mapZoomToBox",
          value: function mapZoomToBox(e) {
            log("mapZoomToBox"); // Find time bounds of selected coordinates

            var bounds = this.coords.reduce(function (t, c) {
              if (e.boxZoomBounds.contains(c.position)) {
                t.from = Math.min(t.from, c.timestamp);
                t.to = Math.max(t.to, c.timestamp);
              }

              return t;
            }, {
              from: Infinity,
              to: -Infinity
            }); // Set the global time range

            if (isFinite(bounds.from) && isFinite(bounds.to)) {
              // KLUDGE: Create moment objects here to avoid a TypeError that
              //         occurs when Grafana processes normal numbers
              this.timeSrv.setTime({
                from: moment.utc(bounds.from),
                to: moment.utc(bounds.to)
              });
            }

            this.render();
          } // Add the circles and polyline to the map

        }, {
          key: "addDataToMap",
          value: function addDataToMap() {
            var _this5 = this;

            var coords = [[]];
            this.coords.forEach(function (coord, index) {
              if (coord.type == 1) {
                var superchargerIcon = L.icon({
                  iconUrl: 'public/plugins/pr0ps-trackmap-panel/img/tesla_pin.png',
                  iconAnchor: [6, 16],
                  popupAnchor: [0, 0]
                });
                var p = new L.latLng(coord.position);
                var marker = new L.marker(p, {
                  icon: superchargerIcon
                });
                marker.bindPopup(coord.text);
                marker.addTo(_this5.leafMap);

                _this5.superchargerMarks.push(marker);
              } else if (coord.type == 2) {
                var superchargerIcon = L.icon({
                  iconUrl: 'public/plugins/pr0ps-trackmap-panel/img/charger_pin.png',
                  iconAnchor: [6, 16],
                  popupAnchor: [0, 0]
                });
                var p = new L.latLng(coord.position);
                var marker = new L.marker(p, {
                  icon: superchargerIcon
                });
                marker.bindPopup(coord.text);
                marker.addTo(_this5.leafMap);

                _this5.superchargerMarks.push(marker);
              } else if (coord.type == 3) {
                var superchargerIcon = L.icon({
                  iconUrl: 'public/plugins/pr0ps-trackmap-panel/img/ac_pin.png',
                  iconAnchor: [6, 16],
                  popupAnchor: [0, 0]
                });
                var p = new L.latLng(coord.position);
                var marker = new L.marker(p, {
                  icon: superchargerIcon
                });
                marker.bindPopup(coord.text);
                marker.addTo(_this5.leafMap);

                _this5.superchargerMarks.push(marker);
              } else if (index !== 0 && _this5.panel.maxDataPointDelta !== 0) {
                var prevTimestamp = _this5.coords[index - 1].timestamp;

                if (coord.timestamp - prevTimestamp > _this5.panel.maxDataPointDelta * 1000) {
                  coords.push([]); // Start a new polyline
                }
              }

              coords[coords.length - 1].push(coord.position);
            });
            log("addDataToMap");
            coords.forEach(function (polyline) {
              _this5.polylines.push(L.polyline(polyline, {
                color: _this5.panel.lineColor,
                weight: 3
              }).addTo(_this5.leafMap));
            });
            this.zoomToFit();
          }
        }, {
          key: "zoomToFit",
          value: function zoomToFit() {
            log("zoomToFit");

            if (this.panel.autoZoom && this.polylines.length > 0) {
              var bounds = this.polylines[0].getBounds();

              for (var i = 1; i < this.polylines.length; i++) {
                bounds = bounds.extend(this.polylines[i].getBounds());
              }

              if (bounds.isValid()) {
                this.leafMap.fitBounds(bounds);
              } else {
                this.leafMap.setView([0, 0], 1);
              }
            }

            this.render();
          }
        }, {
          key: "refreshPolylines",
          value: function refreshPolylines() {
            this.setupMap();
            this.addDataToMap();
          }
        }, {
          key: "refreshColors",
          value: function refreshColors() {
            var _this6 = this;

            log("refreshColors");
            this.polylines.forEach(function (polyline) {
              polyline.setStyle({
                color: _this6.panel.lineColor
              });
            });

            if (this.hoverMarker) {
              this.hoverMarker.setStyle({
                fillColor: this.panel.pointColor
              });
            }

            this.render();
          }
        }, {
          key: "onDataReceived",
          value: function onDataReceived(data) {
            log("onDataReceived");
            this.setupMap();

            if (data[0].columns != null && data[0].rows != null) {
              for (var i = 0; i < data[0].rows.length; i++) {
                var row = data[0].rows[i];
                if (row[0] == null || row[1] == null || row[0] == 0 || row[1] == 0) continue;
                var t = null;
                var txt = null;

                if (true) {
                  t = row[3];
                  if (t > 0) txt = row[4];
                }

                this.coords.push({
                  position: L.latLng(row[1], row[2]),
                  timestamp: row[0],
                  type: t,
                  text: txt
                });
              }

              this.addDataToMap();
              return;
            }

            if (data.length < 2) {
              // No data or incorrect data, show a world map and abort
              this.leafMap.setView([0, 0], 1);
              this.render();
              return;
            } // begin time series
            // Asumption is that there are an equal number of properly matched timestamps
            // TODO: proper joining by timestamp?


            this.coords.length = 0;
            var lats = data[0].datapoints;
            var lons = data[1].datapoints;
            var types = null;
            if (data.length > 2) types = data[2].datapoints;

            for (var _i = 0; _i < lats.length; _i++) {
              if (lats[_i][0] == null || lons[_i][0] == null || lats[_i][0] == 0 && lons[_i][0] == 0 || lats[_i][1] !== lons[_i][1]) {
                continue;
              }

              var t = null;
              var txt = null;

              if (types != null) {
                t = types[_i][0];
                txt = "";
              }

              this.coords.push({
                position: L.latLng(lats[_i][0], lons[_i][0]),
                timestamp: lats[_i][1],
                type: t,
                text: txt
              });
            }

            this.addDataToMap();
          }
        }, {
          key: "onDataSnapshotLoad",
          value: function onDataSnapshotLoad(snapshotData) {
            log("onSnapshotLoad");
            this.onDataReceived(snapshotData);
          }
        }]);

        return TrackMapCtrl;
      }(MetricsPanelCtrl));

      TrackMapCtrl.templateUrl = 'partials/module.html';
    }
  };
});
//# sourceMappingURL=trackmap_ctrl.js.map
