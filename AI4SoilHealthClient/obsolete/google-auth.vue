<template>
    <div ref="googleLoginBtn" />
</template>

<script>

/**
 * Vue component for Google authentication. Not used in the current version of the application.
 * @component
 * @name GoogleAuth
 * @example
 * <GoogleAuth />
 */

export default {
    name: "GoogleAuth",
    data: () => ({
        showBtn: false,
    }),

    /**
     * Initializes the Google authentication button.
     */
    async mounted() {
        const gClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
        await window.google.accounts.id.initialize({
            client_id: gClientId,
            callback: this.handleCredentialResponse,
            auto_select: true
        });
        await window.google.accounts.id.renderButton(
            this.$refs.googleLoginBtn, {
            ux_mode: 'popup',
            text: 'signin_with', // or 'signup_with' | 'continue_with' | 'signin'
            size: 'medium', // or 'small' | 'medium'
            width: '200', // max width 400
            theme: 'outline', // or 'filled_black' |  'filled_blue'
            logo_alignment: 'left', // or 'center'
            locale: this.$store.locale,
        });
    },

    methods: {
        /**
         * Handles the response from the Google authentication.
         * 
         * @param {Object} response - The response from the Google authentication.
         */
        async handleCredentialResponse(response) {
            try {
                const id_token = response.credential;
                this.$store.userData = await this.api(this.axios.API.post, 'Auth/google', { IdToken: id_token });
                console.dir(this.$store.userData);
                this.$q.localStorage.set('userData', this.$store.userData);
            } catch (error) {
                this.logout();
            }
        },
    },
};
</script>
