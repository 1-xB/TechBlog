window.quill = null;

window.initQuill = () => {
    quill = new Quill('#editor-container', {
        theme: 'snow'
    });
};

window.getQuillHtml = async (apiUrl) => {
    const imgs = quill.root.querySelectorAll("img");
    for (let img of imgs) {
        if (img.src.startsWith("data:image")) {
            const blob = dataURLToBlob(img.src);
            const formData = new FormData();
            formData.append("file", blob, "image.png");
            
            const accessToken = localStorage.getItem("accessToken")

            const uploadUrl = `${apiUrl}/api/images/upload`;
            // send to server
            try {
                const response = await fetch(uploadUrl, {
                    method: "POST",
                    headers : {
                        'Authorization' : `Bearer ${accessToken}`
                    },
                    body: formData,
                });

                if (!response.ok) {
                    console.error(`Image transfer error. Status code: ${response.status}`);
                    img.src = "";
                    continue;
                }
                
                const result = await response.json();
                img.src = result.url; // { url: "https://..." }
            } catch (error) {
                console.error(`Image upload failed: ${error.message}`);
                img.src = "";
            }
        }
    }

    return quill.root.innerHTML;
};

function dataURLToBlob(dataUrl) {
    const arr = dataUrl.split(',');
    const mime = arr[0].match(/:(.*?);/)[1];
    const byteString = atob(arr[1]);
    const byteNumbers = Array.from(byteString, char => char.charCodeAt(0));
    const byteArray = new Uint8Array(byteNumbers);
    return new Blob([byteArray], { type: mime });
}