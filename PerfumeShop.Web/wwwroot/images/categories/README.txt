ANLEITUNG ZUM HINZUFÜGEN DER KATEGORIE-BILDER

Um die Kategorien auf der Startseite mit den gewünschten Bildern zu versehen, führen Sie bitte folgende Schritte aus:

1. Laden Sie die folgenden Bilder herunter:
   - Das Bild des Mannes im blauen Anzug von: https://zuitable.de/products/jersey-anzug-221605-680
   - Das Bild der Frau im goldenen Kleid von: https://de.dreamstime.com/stockfoto-mode-modell-girl-posing-glamour-goldkleid-elegante-frauen-kleid-image76505142
   - Das Bild des Paares (Mann und Frau in eleganter Kleidung) von: https://www.freepik.com/premium-ai-image/fashion-woman-man-couple-luxury-model-girl-silver-dress-holding-man-elegant-suit-provocative-st_376596178.htm

2. Speichern Sie die Bilder in diesem Ordner mit folgenden Namen:
   - men-category.jpg - Für das Männerbild
   - women-category.jpg - Für das Frauenbild
   - unisex-category.jpg - Für das Paar-Bild

3. Öffnen Sie die Datei "PerfumeShop.Web/Views/Home/Index.cshtml" und aktualisieren Sie den Kategorien-Abschnitt:
   - Ersetzen Sie den Platzhalter für die Männerkategorie mit:
     <div class="category-item" style="position: relative; height: 400px; overflow: hidden; margin-bottom: 30px;">
         <img src="~/images/categories/men-category.jpg" alt="Herren" style="width: 100%; height: 100%; object-fit: cover;">
         <div style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.4); display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; padding: 20px;">
             <h3 style="color: #fff; font-size: 28px; margin-bottom: 15px; text-transform: uppercase; font-weight: 600;">Herren</h3>
             <p style="color: #fff; margin-bottom: 20px;">Markante und charaktervolle Düfte für Ihn</p>
             <a href="#" class="primary-btn">ENTDECKEN</a>
         </div>
     </div>

   - Ersetzen Sie den Platzhalter für die Frauenkategorie mit:
     <div class="category-item" style="position: relative; height: 400px; overflow: hidden; margin-bottom: 30px;">
         <img src="~/images/categories/women-category.jpg" alt="Damen" style="width: 100%; height: 100%; object-fit: cover;">
         <div style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.4); display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; padding: 20px;">
             <h3 style="color: #fff; font-size: 28px; margin-bottom: 15px; text-transform: uppercase; font-weight: 600;">Damen</h3>
             <p style="color: #fff; margin-bottom: 20px;">Elegante und verführerische Düfte für Sie</p>
             <a href="#" class="primary-btn">ENTDECKEN</a>
         </div>
     </div>

Hinweis: Die aktuellen Platzhalter verwenden farbige Hintergründe, bis die eigentlichen Bilder hinzugefügt werden. 