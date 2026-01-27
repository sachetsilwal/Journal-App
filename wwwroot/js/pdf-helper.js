/**
 * PDF Helper for Journals App
 * Uses html2pdf.js to generate PDF from HTML element and returns base64
 */

window.pdfHelper = {
    generatePdfBase64: async function (elementId, filename) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Element not found:', elementId);
            return null;
        }

        const opt = {
            margin: [10, 10, 10, 10], // top, left, bottom, right in mm
            filename: filename || 'journal-entry.pdf',
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: { scale: 2, useCORS: true, logging: false },
            jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' },
            pagebreak: { mode: ['avoid-all', 'css', 'legacy'] }
        };

        try {
            // Generate PDF as blob, then convert to base64
            const pdfBlob = await html2pdf().set(opt).from(element).output('blob');

            return new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onloadend = () => {
                    // Extract base64 string (remove data:application/pdf;base64, prefix)
                    const base64String = reader.result.split(',')[1];
                    resolve(base64String);
                };
                reader.onerror = reject;
                reader.readAsDataURL(pdfBlob);
            });
        } catch (error) {
            console.error('PDF Generation Error:', error);
            return null;
        }
    }
};
