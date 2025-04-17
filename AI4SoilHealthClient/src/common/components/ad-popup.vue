<template>
  <div class="aad-popup">
    <div class="aad-content">
      <!-- AdSense Ad -->
      <ins class="adsbygoogle"
           style="display:block"
           :data-ad-client="$store.adClient"
           :data-ad-slot="$store.adSlot"
           data-ad-format="auto"
           data-full-width-responsive="true"
           data-adtest="on">

      </ins>
      <!-- Close Button -->
      <q-btn icon="close" @click="$store.showAd = false" flat label="Close"/>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      visible: false,
    };
  },
  mounted () {
    this.$nextTick(() => {
      // Trigger Google AdSense ad display
      if (window.adsbygoogle) {
        (window.adsbygoogle = window.adsbygoogle || []).push({});
      }
    });
  },
};
</script>

<style>
.ad-popup {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}
.ad-content {
  background: white;
  padding: 20px;
  text-align: center;
}
</style>