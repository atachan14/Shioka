# Adopted forward walk cycle
Source: user Downloads/Shioka-forward-walk-approved-4frames-20261024.zip. Manifest initially held import during v0.1; later user request to reflect prepared FrontWalk art lifts that hold. PNG bytes are preserved, checked against source SHA256. Order01 contactA ->02 passing-near ->03 contactB ->04 passing-far ->01, no duplicated terminal frame. True RGBA transparency inspected; no GIF gray background imported.

ForwardWalk.asset now holds these four sprites. Provisional game timing:4 simulation frames each,16F loop; editable in the asset, independent of unchanged walk speed. This is not the300ms/frame review GIF timing. All use700PPU and the same character body calibration; no individual height normalization. RootX pivot0.55 preserved; ground sole row >=alpha128 determines vertical pivot1500/1503/1471/1485 in1536px canvas. Small source pose/armor continuity and foot sliding may need further artistic/timing tuning. The older Walk.png remains intact.

WeakP: old approved single active pose is present; newer ten-pose source not located locally at audit. BackAirDash PNG exists but its adopted mechanics/timing are not established in current code; not silently mapped to forward air dash. New5MP candidates are not imported.

Timing clarification: 4F per pose was chosen as an implementation provisional value, not a user-approved cycle or a preservation of prior timing. Before import there was one held sprite (nominal hold1F, visually static); after import16F at60Hz (~267ms/cycle). The review GIF1.2s cycle is not game timing. Final walk-cycle duration remains a confirmation item.

WeakP follow-up: v14 ZIP location was identified in Library (libfile_c449ce57d4ec8191a05cdfb2b55714dc); entire-version adoption and combat-phase mapping are still unverified, so no import. Ten-pose preview660ms is not adopted move timing.
