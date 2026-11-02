use image::imageops::FilterType;
use image::ImageReader;
use std::fs;
use std::path::{Path, PathBuf};
use uuid::Uuid;

/// 图片证据落盘服务：统一压缩到最长边 1920px、JPEG 85% 质量
pub struct ImageService;

const MAX_EDGE: u32 = 1920;
const JPEG_QUALITY: u8 = 85;

impl ImageService {
    pub fn resolve(file_path: &str) -> Result<PathBuf, String> {
        let name = file_path.rsplit(['/', '\\']).next().filter(|n| !n.is_empty() && *n != "." && *n != "..").ok_or("无效图片路径")?;
        Ok(crate::db::Db::get_images_dir()?.join(name))
    }

    /// 把外部图片导入到应用私有目录，返回 (存储文件名, 绝对路径)
    pub fn import(source: &str) -> Result<(String, String), String> {
        let src = Path::new(source);
        if !src.exists() {
            return Err(format!("图片不存在: {}", source));
        }
        let dir = crate::db::Db::get_images_dir()?;
        let file_name = format!("{}.jpg", Uuid::new_v4());
        let dest = dir.join(&file_name);

        let img = ImageReader::open(src)
            .map_err(|e| format!("无法读取图片: {}", e))?
            .with_guessed_format()
            .map_err(|e| format!("无法识别图片格式: {}", e))?
            .decode()
            .map_err(|e| format!("无法解码图片: {}", e))?;

        let (w, h) = (img.width(), img.height());
        let img = if w > MAX_EDGE || h > MAX_EDGE {
            img.resize(MAX_EDGE, MAX_EDGE, FilterType::Lanczos3)
        } else {
            img
        };

        let rgb = img.to_rgb8();
        let mut out = fs::File::create(&dest).map_err(|e| format!("无法写入图片: {}", e))?;
        let mut encoder =
            image::codecs::jpeg::JpegEncoder::new_with_quality(&mut out, JPEG_QUALITY);
        encoder
            .encode_image(&rgb)
            .map_err(|e| format!("图片编码失败: {}", e))?;

        Ok((file_name.clone(), file_name))
    }

    pub fn import_avatar(source:&str)->Result<(String,String),String>{
        let img=ImageReader::open(source).map_err(|e|e.to_string())?.with_guessed_format().map_err(|e|e.to_string())?.decode().map_err(|e|format!("无法解码头像：{}",e))?;
        if !img.color().has_alpha(){return Self::import(source);}
        let img=if img.width()>MAX_EDGE||img.height()>MAX_EDGE{img.resize(MAX_EDGE,MAX_EDGE,FilterType::Lanczos3)}else{img};
        let name=format!("{}.png",Uuid::new_v4());
        img.save_with_format(crate::db::Db::get_images_dir()?.join(&name),image::ImageFormat::Png).map_err(|e|e.to_string())?;
        Ok((name.clone(),name))
    }

    /// 删除一张已存储的图片文件（忽略不存在的情况）
    pub fn remove(file_path: &str) -> Result<(), String> {
        let p = Self::resolve(file_path)?;
        if p.exists() {
            fs::remove_file(&p).map_err(|e| format!("删除图片失败: {}", e))?;
        }
        Ok(())
    }

    pub fn thumbnail(path: &str) -> Result<String,String> {
        use base64::Engine;
        let image = image::open(Self::resolve(path)?).map_err(|e| e.to_string())?.thumbnail(360, 270).to_rgb8();
        let mut bytes = Vec::new();
        image::codecs::jpeg::JpegEncoder::new_with_quality(&mut bytes,75).encode_image(&image).map_err(|e| e.to_string())?;
        Ok(format!("data:image/jpeg;base64,{}",base64::engine::general_purpose::STANDARD.encode(bytes)))
    }

    /// 把已存储图片读成 data URL，供前端 <img> 直接显示（避免 file:// 协议限制）
    pub fn to_data_url(file_path: &str) -> Result<String, String> {
        use base64::Engine;
        let bytes = fs::read(Self::resolve(file_path)?).map_err(|e| format!("读取图片失败: {}", e))?;
        let b64 = base64::engine::general_purpose::STANDARD.encode(bytes);
        let mime=if file_path.to_ascii_lowercase().ends_with(".png"){"image/png"}else{"image/jpeg"};
        Ok(format!("data:{};base64,{}",mime,b64))
    }
}
