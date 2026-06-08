import { useState } from 'react';
import Apis, { endpoints } from './configs/Apis';
import Swal from 'sweetalert2';

function App() {
  const [userFile, setUserFile] = useState(null);
  const [caFile, setCaFile] = useState(null);
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleUpload = async (e) => {
    e.preventDefault(); // chặn tải lại trang 

    if (!userFile || !caFile) { // kiểm tra đầu vào
      Swal.mixin({
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
          toast.onmouseenter = Swal.stopTimer;
          toast.onmouseleave = Swal.resumeTimer;
        }
      }).fire({
        icon: "error",
        title: "Vui lòng chọn đầy đủ file!"
      });

      return;
    }

    setLoading(true); // bật trạng thái chờ
    setError(null); // xóa lỗi cũ
    setResult(null); // xóa kết quả cũ

    try {
      const formData = new FormData(); // đóng gói tệp tin 

      formData.append("userFile", userFile);
      formData.append("caFile", caFile);

      const res = await Apis.post(endpoints["certificate-info"], formData); // gọi API xử lý

      setResult(res.data); // đổ dữ liệu vào kết quả
      Swal.mixin({
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
          toast.onmouseenter = Swal.stopTimer;
          toast.onmouseleave = Swal.resumeTimer;
        }
      }).fire({
        icon: "success",
        title: "Xử lý dữ liệu thành công"
      });


    } catch (err) { // quăng lỗi nếu có
      Swal.fire({
        icon: "error",
        title: "Lỗi hệ thống",
        text: err.message,
      });
    } finally {
      setLoading(false); // tắt trạng thái chờ 
    }
  };

  // Hàm trả về class Tailwind cho từng trạng thái màu sắc công nghiệp
  const getBadgeClass = (status) => {
    if (status === "VALID" || status === "GOOD")
      return "px-3 py-1 rounded-full text-sm font-semibold bg-green-100 text-green-700 border border-green-200";
    if (status === "REVOKED")
      return "px-3 py-1 rounded-full text-sm font-semibold bg-red-100 text-red-700 border border-red-200";
    if (status === "UNAUTHORIZED_BY_CA")
      return "px-3 py-1 rounded-full text-sm font-semibold bg-amber-100 text-amber-700 border border-amber-200";
    return "px-3 py-1 rounded-full text-sm font-semibold bg-gray-100 text-gray-700";
  };

  return (
    <div className="min-h-screen bg-slate-50 py-10 px-4 font-sans text-slate-800">
      <div className="max-w-3xl mx-auto">

        {/* Tiêu đề chính */}
        <div className="text-center mb-8">
          <h1 className="text-3xl font-extrabold text-blue-600 tracking-tight">PKI Certificate Tool</h1>
          <p className="text-slate-500 mt-2 text-sm">Hệ thống hỗ trợ kiểm tra CRL và OCSP</p>
        </div>

        {/* Form Upload certificate */}
        <form onSubmit={handleUpload} className="bg-white p-6 rounded-xl shadow-md border border-slate-100 gap-5 flex flex-col">
          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">1. User Certificate File (.cer, .crt)</label>
            <input
              type="file"
              onChange={(e) => setUserFile(e.target.files[0])}
              accept=".cer,.crt"
              className="block w-full text-sm text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100 border border-slate-200 rounded-lg p-2"
            />
          </div>

          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">2. CA Certificate File (.cer, .crt)</label>
            <input
              type="file"
              onChange={(e) => setCaFile(e.target.files[0])}
              accept=".cer,.crt"
              className="block w-full text-sm text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100 border border-slate-200 rounded-lg p-2"
            />
          </div>
          <button
            type="submit"
            disabled={loading}
            className={`w-full py-3 px-4 rounded-lg font-medium text-white transition-all ${loading ? 'bg-slate-400 cursor-not-allowed' : 'bg-blue-600 hover:bg-blue-700 shadow-sm shadow-blue-200'}`}
          >
            {loading ? (
              <div className="flex items-center justify-center gap-2">
                <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                <span>Đang xử lý dữ liệu ...</span>
              </div>
            ) : "Kiểm tra trạng thái chứng chỉ"}
          </button>
        </form>

        {/* Hiển thị lỗi hệ thống nếu có */}
        {error && (
          <div className="mt-5 p-4 bg-red-50 text-red-700 rounded-lg border-l-4 border-red-500 text-sm">
            <span className="font-bold">Lỗi:</span> {error}
          </div>
        )}

        {/* Bảng kết quả hiển thị thông tin bóc tách */}
        {result && (
          <div className="mt-8 bg-white rounded-xl shadow-lg border border-slate-100 overflow-hidden animate-fade-in">
            <div className="bg-gradient-to-r from-blue-700 to-indigo-600 px-6 py-4">
              <h2 className="text-lg font-bold text-white">Kết quả phân tích</h2>
            </div>

            <div className="p-6">
              <div className="divide-y divide-slate-100">

                <div className="py-3.5 flex flex-col sm:flex-row sm:items-center justify-between gap-1">
                  <span className="font-semibold text-slate-600 w-44">Nhà cung cấp (CA):</span>
                  <span className="text-blue-700 font-bold text-base flex-1">{result.caProvider}</span>
                </div>

                <div className="py-3.5 flex flex-col sm:flex-row gap-1">
                  <span className="font-semibold text-slate-600 w-44 shrink-0">Chủ thể (Subject):</span>
                  <span className="text-sm text-slate-700 bg-slate-50 p-2 rounded border border-slate-100 font-mono break-all flex-1">{result.subject}</span>
                </div>

                <div className="py-3.5 flex flex-col sm:flex-row gap-1">
                  <span className="font-semibold text-slate-600 w-44 shrink-0">Bên cấp phát (Issuer):</span>
                  <span className="text-sm text-slate-700 font-mono break-all flex-1">{result.issuer}</span>
                </div>

                <div className="py-3.5 flex flex-col sm:flex-row sm:items-center justify-between gap-1">
                  <span className="font-semibold text-slate-600 w-44">Số Serial (Hex):</span>
                  <span className="font-mono text-slate-900 bg-slate-100 px-2 py-0.5 rounded-full text-sm border border-slate-300">{result.serialNumber}</span>
                </div>

                <div className="py-3.5 flex flex-col sm:flex-row sm:items-center justify-between gap-1">
                  <span className="font-semibold text-slate-600 w-44">Có hiệu lực từ:</span>
                  <span className="text-sm text-slate-600 font-medium">{result.validFrom}</span>
                </div>

                <div className="py-3.5 flex flex-col sm:flex-row sm:items-center justify-between gap-1">
                  <span className="font-semibold text-slate-600 w-44">Hết hạn vào:</span>
                  <span className="text-sm text-slate-600 font-medium">{result.validTo}</span>
                </div>

                <div className="py-3.5 flex flex-col sm:flex-row sm:items-center gap-2">
                  <span className="font-semibold text-slate-600 w-44">Trạng thái CRL:</span>
                  <span className={getBadgeClass(result.crlStatus)}>{result.crlStatus}</span>
                </div>

                <div className="py-3.5 flex flex-col sm:flex-row gap-2">
                  <span className="font-semibold text-slate-600 w-44 shrink-0">Trạng thái OCSP:</span>
                  <div className="flex flex-col gap-1.5 items-start">

                    {/* Kiểm tra nếu chuỗi chứa số 6 hoặc lỗi 6 thì ép hiển thị chữ UNAUTHORIZED_BY_CA, ngược lại giữ nguyên GOOD/REVOKED */}
                    <span className={getBadgeClass(result.ocspStatus.includes("6") ? "UNAUTHORIZED_BY_CA" : result.ocspStatus)}>
                      {result.ocspStatus.includes("6") ? "UNAUTHORIZED_BY_CA" : result.ocspStatus}
                    </span>

                    {/* Điều kiện ẩn/hiện dòng nhắc nhở: Chỉ cần chuỗi trả về từ Backend có chứa số 6 là hiện lên */}
                    {(result.ocspStatus === "UNAUTHORIZED_BY_CA" || result.ocspStatus.includes("6")) && (
                      <p className="text-xs text-amber-600 italic max-w-lg mt-1">
                        * Giao thức OCSP bị nhà cung cấp chặn truy vấn tự do (Cần Signed Request). Vui lòng đối chiếu trạng thái làm chuẩn dựa trên kết quả danh sách thu hồi CRL ở trên.
                      </p>
                    )}

                  </div>
                </div>

              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default App;







// import { useState, useEffect } from 'react';
// import Apis, { endpoints } from './configs/Apis';
// import Swal from 'sweetalert2';

// function App() {
//   const [userFile, setUserFile] = useState(null);
//   const [caFile, setCaFile] = useState(null);
//   const [result, setResult] = useState(null);
//   const [loading, setLoading] = useState(false);
//   const [error, setError] = useState(null);
  
//   // Khởi tạo trạng thái quản lý danh sách lịch sử quét từ bộ nhớ trình duyệt
//   const [history, setHistory] = useState(() => {
//     const saved = localStorage.getItem('pki_scan_history');
//     return saved ? JSON.parse(saved) : [];
//   });

//   // Đồng bộ hóa dữ liệu lịch sử tự động mỗi khi có lượt quét mới
//   useEffect(() => {
//     localStorage.setItem('pki_scan_history', JSON.stringify(history));
//   }, [history]);

//   const handleUpload = async (e) => {
//     e.preventDefault();

//     if (!userFile || !caFile) {
//       Swal.mixin({
//         toast: true,
//         position: "top-end",
//         showConfirmButton: false,
//         timer: 3000,
//         timerProgressBar: true,
//       }).fire({
//         icon: "error",
//         title: "Vui lòng chọn đầy đủ file!"
//       });
//       return;
//     }

//     setLoading(true);
//     setError(null);
//     setResult(null);

//     try {
//       const formData = new FormData();
//       formData.append("userFile", userFile);
//       formData.append("caFile", caFile);

//       const res = await Apis.post(endpoints["certificate-info"], formData);
//       const data = res.data;

//       setResult(data);

//       // Tự động lưu bản ghi mới vào đầu danh sách lịch sử (Tối đa lưu 5 bản ghi gần nhất)
//       const newHistoryItem = {
//         id: Date.now(),
//         time: new Date().toLocaleTimeString() + ' ' + new Date().toLocaleDateString(),
//         caProvider: data.caProvider || "Không rõ",
//         subject: data.subject || "Không rõ",
//         crlStatus: data.crlStatus,
//         ocspStatus: data.ocspStatus,
//         rawData: data
//       };
//       setHistory(prev => [newHistoryItem, ...prev.slice(0, 4)]);

//       Swal.mixin({
//         toast: true,
//         position: "top-end",
//         showConfirmButton: false,
//         timer: 3000,
//         timerProgressBar: true,
//       }).fire({
//         icon: "success",
//         title: "Xử lý dữ liệu thành công"
//       });

//     } catch (err) {
//       Swal.fire({
//         icon: "error",
//         title: "Lỗi hệ thống",
//         text: err.response?.data?.message || err.message,
//       });
//       setError(err.response?.data?.message || err.message);
//     } finally {
//       setLoading(false);
//     }
//   };

//   // Hàm hỗ trợ xem lại kết quả cũ từ bảng lịch sử mà không cần gọi lại API mạng
//   const handleViewHistoryItem = (item) => {
//     setResult(item.rawData);
//     setError(null);
//     Swal.mixin({
//       toast: true,
//       position: "top-end",
//       showConfirmButton: false,
//       timer: 2000,
//     }).fire({
//       icon: "info",
//       title: "Đang hiển thị kết quả từ lịch sử kiểm tra"
//     });
//   };

//   const getBadgeClass = (status) => {
//     if (!status) return "px-3 py-1 rounded-full text-sm font-semibold bg-gray-100 text-gray-700";
//     if (status === "VALID" || status === "GOOD")
//       return "px-3 py-1 rounded-full text-sm font-semibold bg-green-100 text-green-700 border border-green-200";
//     if (status === "REVOKED")
//       return "px-3 py-1 rounded-full text-sm font-semibold bg-red-100 text-red-700 border border-red-200";
//     if (status === "UNAUTHORIZED_BY_CA" || status.includes("6"))
//       return "px-3 py-1 rounded-full text-sm font-semibold bg-amber-100 text-amber-700 border border-amber-200";
//     return "px-3 py-1 rounded-full text-sm font-semibold bg-gray-100 text-gray-700 border border-gray-200";
//   };

//   return (
//     <div className="min-h-screen bg-slate-50 py-10 px-4 font-sans text-slate-800">
//       <div className="max-w-4xl mx-auto">

//         {/* Tiêu đề ứng dụng */}
//         <div className="text-center mb-8">
//           <h1 className="text-3xl font-extrabold text-blue-600 tracking-tight">PKI Certificate Tool</h1>
//           <p className="text-slate-500 mt-2 text-sm">Hệ thống hỗ trợ kiểm tra thời gian thực CRL và OCSP</p>
//         </div>

//         {/* Khung tương tác chính */}
//         <div className="grid grid-cols-1 gap-8">
          
//           <form onSubmit={handleUpload} className="bg-white p-6 rounded-xl shadow-md border border-slate-100 gap-6 flex flex-col">
            
//             {/* Cải tiến 1: Khu vực hiển thị tệp tin kéo thả/lọc trực quan */}
//             <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
//               <div className="relative group border-2 border-dashed border-slate-200 hover:border-blue-400 rounded-xl p-4 bg-slate-50/50 transition-all">
//                 <label className="block text-sm font-bold text-slate-700 mb-2 cursor-pointer">
//                   1. User Certificate File
//                   <input
//                     type="file"
//                     onChange={(e) => setUserFile(e.target.files[0])}
//                     accept=".cer,.crt"
//                     className="hidden"
//                   />
//                   <div className="mt-2 flex items-center justify-center py-4 bg-white rounded-lg border border-slate-100 shadow-sm group-hover:bg-blue-50/30 transition-all">
//                     {userFile ? (
//                       <div className="flex items-center gap-2 px-2 text-blue-600 font-medium text-sm truncate">
//                         <span className="truncate">{userFile.name}</span>
//                       </div>
//                     ) : (
//                       <span className="text-sm text-slate-400">Chọn hoặc kéo thả file .cer/.crt</span>
//                     )}
//                   </div>
//                 </label>
//               </div>

//               <div className="relative group border-2 border-dashed border-slate-200 hover:border-blue-400 rounded-xl p-4 bg-slate-50/50 transition-all">
//                 <label className="block text-sm font-bold text-slate-700 mb-2 cursor-pointer">
//                   2. CA Certificate File
//                   <input
//                     type="file"
//                     onChange={(e) => setCaFile(e.target.files[0])}
//                     accept=".cer,.crt"
//                     className="hidden"
//                   />
//                   <div className="mt-2 flex items-center justify-center py-4 bg-white rounded-lg border border-slate-100 shadow-sm group-hover:bg-blue-50/30 transition-all">
//                     {caFile ? (
//                       <div className="flex items-center gap-2 px-2 text-indigo-600 font-medium text-sm truncate">
//                         <span className="truncate">{caFile.name}</span>
//                       </div>
//                     ) : (
//                       <span className="text-sm text-slate-400">Chọn hoặc kéo thả file .cer/.crt</span>
//                     )}
//                   </div>
//                 </label>
//               </div>
//             </div>

//             {/* Cải tiến 2: Thanh tiến trình Stepper tự động kích hoạt khi Loading */}
//             {loading && (
//               <div className="bg-blue-50/50 border border-blue-100 rounded-xl p-4 animate-pulse">
//                 <div className="flex justify-between max-w-md mx-auto text-xs font-semibold text-slate-500">
//                   <div className="flex flex-col items-center gap-1.5 text-blue-600">
//                     <div className="w-5 h-5 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold">1</div>
//                     <span>Đọc tệp tin</span>
//                   </div>
//                   <div className="h-0.5 bg-blue-300 flex-1 self-center mx-2 mb-4"></div>
//                   <div className="flex flex-col items-center gap-1.5 text-blue-600">
//                     <div className="w-5 h-5 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold animate-spin border-2 border-white border-t-transparent"></div>
//                     <span>Xác thực CRL</span>
//                   </div>
//                   <div className="h-0.5 bg-slate-200 flex-1 self-center mx-2 mb-4"></div>
//                   <div className="flex flex-col items-center gap-1.5">
//                     <div className="w-5 h-5 rounded-full bg-slate-200 text-slate-600 flex items-center justify-center font-bold">3</div>
//                     <span>Giao tiếp OCSP</span>
//                   </div>
//                 </div>
//               </div>
//             )}

//             <button
//               type="submit"
//               disabled={loading}
//               className={`w-full py-3.5 px-4 rounded-xl font-semibold text-white transition-all ${loading ? 'bg-slate-400 cursor-not-allowed' : 'bg-blue-600 hover:bg-blue-700 shadow-md shadow-blue-200'}`}
//             >
//               {loading ? "Hệ thống đang phân tích cấu trúc ASN.1..." : "Bắt đầu kiểm tra trạng thái chứng chỉ"}
//             </button>
//           </form>

//           {error && (
//             <div className="p-4 bg-red-50 text-red-700 rounded-xl border-l-4 border-red-500 text-sm shadow-sm">
//               <span className="font-bold">Lỗi vận hành:</span> {error}
//             </div>
//           )}

//           {/* Khối hiển thị bảng kết quả dữ liệu */}
//           {result && (
//             <div className="bg-white rounded-xl shadow-lg border border-slate-100 overflow-hidden animate-fade-in">
//               <div className="bg-gradient-to-r from-blue-700 to-indigo-600 px-6 py-4 flex justify-between items-center">
//                 <h2 className="text-lg font-bold text-white">Kết quả phân tích hệ thống</h2>
//                 <span className="text-xs text-blue-50 bg-blue-800/30 px-2.5 py-1 rounded-full border border-blue-400/30 font-mono font-medium">Serial: {result.serialNumber}</span>
//               </div>

//               <div className="p-6">
//                 <div className="divide-y divide-slate-100">
//                   <div className="py-3 flex flex-col sm:flex-row sm:items-center justify-between gap-1">
//                     <span className="font-semibold text-slate-500 text-sm w-44">Nhà cung cấp (CA):</span>
//                     <span className="text-blue-700 font-bold flex-1">{result.caProvider}</span>
//                   </div>

//                   <div className="py-3 flex flex-col sm:flex-row gap-1">
//                     <span className="font-semibold text-slate-500 text-sm w-44 shrink-0">Chủ thể (Subject):</span>
//                     <span className="text-xs text-slate-700 bg-slate-50 p-2.5 rounded-lg border border-slate-100 font-mono break-all flex-1 leading-relaxed">{result.subject}</span>
//                   </div>

//                   <div className="py-3 flex flex-col sm:flex-row gap-1">
//                     <span className="font-semibold text-slate-500 text-sm w-44 shrink-0">Bên cấp phát (Issuer):</span>
//                     <span className="text-xs text-slate-600 font-mono break-all flex-1">{result.issuer}</span>
//                   </div>

//                   <div className="py-3 flex flex-col sm:flex-row sm:items-center justify-between gap-1">
//                     <span className="font-semibold text-slate-500 text-sm w-44">Có hiệu lực từ:</span>
//                     <span className="text-sm text-slate-700 font-medium">{result.validFrom}</span>
//                   </div>

//                   <div className="py-3 flex flex-col sm:flex-row sm:items-center justify-between gap-1">
//                     <span className="font-semibold text-slate-500 text-sm w-44">Hết hạn vào:</span>
//                     <span className="text-sm text-slate-700 font-medium">{result.validTo}</span>
//                   </div>

//                   <div className="py-3.5 flex flex-col sm:flex-row sm:items-center gap-4">
//                     <span className="font-semibold text-slate-500 text-sm w-44">Trạng thái CRL:</span>
//                     <span className={getBadgeClass(result.crlStatus)}>{result.crlStatus}</span>
//                   </div>

//                   <div className="py-3.5 flex flex-col sm:flex-row gap-4">
//                     <span className="font-semibold text-slate-500 text-sm w-44 shrink-0">Trạng thái OCSP:</span>
//                     <div className="flex flex-col gap-2 items-start">
//                       <span className={getBadgeClass(result.ocspStatus.includes("6") ? "UNAUTHORIZED_BY_CA" : result.ocspStatus)}>
//                         {result.ocspStatus.includes("6") ? "UNAUTHORIZED_BY_CA" : result.ocspStatus}
//                       </span>
//                       {(result.ocspStatus === "UNAUTHORIZED_BY_CA" || result.ocspStatus.includes("6")) && (
//                         <p className="text-xs text-amber-600 italic max-w-xl mt-0.5 leading-relaxed bg-amber-50 p-2 rounded-lg border border-amber-100">
//                           * Giao thức OCSP bị nhà cung cấp chặn truy vấn tự do (Cần Signed Request). Vui lòng đối chiếu trạng thái làm chuẩn dựa trên kết quả danh sách thu hồi CRL ở trên.
//                         </p>
//                       )}
//                     </div>
//                   </div>
//                 </div>
//               </div>
//             </div>
//           )}

//           {/* Cải tiến 3: Khu vực quản lý lịch sử vận hành */}
//           <div className="bg-white p-5 rounded-xl shadow-md border border-slate-100">
//             <div className="flex justify-between items-center mb-4">
//               <h3 className="text-sm font-bold text-slate-700 flex items-center gap-2">
//                 Lịch sử kiểm tra gần đây
//               </h3>
//               {history.length > 0 && (
//                 <button 
//                   onClick={() => { setHistory([]); localStorage.removeItem('pki_scan_history'); }}
//                   className="text-xs text-red-500 hover:text-red-700 font-medium transition-all"
//                 >
//                   Xóa lịch sử
//                 </button>
//               )}
//             </div>

//             {history.length === 0 ? (
//               <div className="text-center py-6 text-slate-400 text-xs italic bg-slate-50 rounded-lg border border-slate-100">
//                 Chưa có dữ liệu vận hành nào được ghi nhận.
//               </div>
//             ) : (
//               <div className="overflow-x-auto">
//                 <table className="w-full text-left text-xs border-collapse">
//                   <thead>
//                     <tr className="bg-slate-50 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-100">
//                       <th className="py-2 px-3">Thời gian</th>
//                       <th className="py-2 px-3">Nhà cung cấp</th>
//                       <th className="py-2 px-3">CRL</th>
//                       <th className="py-2 px-3">OCSP</th>
//                       <th className="py-2 px-3 text-right">Thao tác</th>
//                     </tr>
//                   </thead>
//                   <tbody className="divide-y divide-slate-100 text-slate-600">
//                     {history.map((item) => (
//                       <tr key={item.id} className="hover:bg-slate-50/80 transition-all">
//                         <td className="py-2.5 px-3 font-mono text-[11px] text-slate-400">{item.time}</td>
//                         <td className="py-2.5 px-3 font-semibold text-slate-700 truncate max-w-[150px]">{item.caProvider}</td>
//                         <td className="py-2.5 px-3">
//                           <span className={item.crlStatus === "VALID" ? "text-green-600 font-medium" : "text-red-600 font-medium"}>{item.crlStatus}</span>
//                         </td>
//                         <td className="py-2.5 px-3">
//                           <span className={(item.ocspStatus === "GOOD" || item.ocspStatus === "VALID") ? "text-green-600 font-medium" : item.ocspStatus === "REVOKED" ? "text-red-600 font-medium" : "text-amber-600 font-medium"}>
//                             {item.ocspStatus.includes("6") ? "UNAUTH_6" : item.ocspStatus}
//                           </span>
//                         </td>
//                         <td className="py-2.5 px-3 text-right">
//                           <button
//                             onClick={() => handleViewHistoryItem(item)}
//                             className="px-2 py-1 text-[11px] font-semibold text-blue-600 bg-blue-50 border border-blue-100 rounded-md hover:bg-blue-600 hover:text-white transition-all"
//                           >
//                             Xem lại
//                           </button>
//                         </td>
//                       </tr>
//                     ))}
//                   </tbody>
//                 </table>
//               </div>
//             )}
//           </div>

//         </div>
//       </div>
//     </div>
//   );
// }

// export default App;