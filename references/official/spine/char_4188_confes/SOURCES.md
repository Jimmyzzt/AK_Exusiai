# CONFESS-47 Spine source

Source: [PRTS CONFESS-47](https://prts.wiki/w/CONFESS-47), using its embedded
`char_4188_confes` Spine viewer. The source index is
`https://torappu.prts.wiki/assets/char_spine/char_4188_confes/meta.json`.

`tools/fetch_confess47_spine.py` downloads the original front, back, and build
`.skel`, `.atlas`, and texture `.png` files without modifying them. These
original assets remain in this folder. Runtime copies are prepared by
`tools/prepare_confess47_spine.py` using SpineSkeletonDataConverter 4.2.43.
`SHA256SUMS` records hashes of the downloaded source files.
The build texture is resized from 684×684 to the atlas-declared 1024×1024;
front and back already match their atlas dimensions. The original build image
is preserved here for comparison.

The downloaded source is Arknights game art. Its availability on PRTS does not
change the rights held by the original owner.
